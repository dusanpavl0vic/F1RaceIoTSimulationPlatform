using System.Text;
using System.Text.Json;
using F1.FeedReplay.Service.Application.Commands;
using F1.FeedReplay.Service.Application.Contracts;
using F1.FeedReplay.Service.Application.Models;
using F1.FeedReplay.Service.Domain.Models;
using F1.FeedReplay.Service.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace F1.FeedReplay.Service.Infrastructure.Bootstrap;

public sealed class ReplayBootstrapper(
    IHttpClientFactory httpClientFactory,
    IWebHostEnvironment hostEnvironment,
    IOptions<ReplayBootstrapOptions> bootstrapOptions,
    IOptions<ReplayServiceOptions> replayServiceOptions,
    IReplayConfigurationLoader replayConfigurationLoader,
    IReplayCoordinator replayCoordinator,
    ILogger<ReplayBootstrapper> logger) : IReplayBootstrapper
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly IWebHostEnvironment _hostEnvironment = hostEnvironment;
    private readonly ReplayBootstrapOptions _bootstrapOptions = bootstrapOptions.Value;
    private readonly ReplayServiceOptions _replayServiceOptions = replayServiceOptions.Value;
    private readonly IReplayConfigurationLoader _replayConfigurationLoader = replayConfigurationLoader;
    private readonly IReplayCoordinator _replayCoordinator = replayCoordinator;
    private readonly ILogger<ReplayBootstrapper> _logger = logger;

    public async Task<ReplayBootstrapResult> BootstrapAsync(
        string? configurationPath,
        string? indexUrl,
        bool downloadFeeds,
        bool forceDownload,
        bool loadAfterDownload,
        bool startAfterLoad,
        CancellationToken cancellationToken)
    {
        var storageRootPath = ResolveStorageRootPath();
        Directory.CreateDirectory(storageRootPath);

        var resolvedConfigurationPath = ResolveConfigurationPath(configurationPath, storageRootPath);
        var resolvedIndexUrl = ResolveIndexUrl(indexUrl);
        var downloadedFeeds = new List<string>();
        var skippedFeeds = new List<string>();

        if (downloadFeeds)
        {
            var indexPayload = await DownloadIndexPayloadAsync(resolvedIndexUrl, cancellationToken);
            var indexPath = Path.Combine(storageRootPath, _bootstrapOptions.IndexFileName);
            await File.WriteAllTextAsync(indexPath, indexPayload, cancellationToken);

            var indexDocument = JsonSerializer.Deserialize<IndexDocument>(indexPayload, SerializerOptions)
                ?? throw new InvalidOperationException("Downloaded Index.json is empty or invalid.");

            await GenerateReplayConfigurationAsync(indexDocument, resolvedIndexUrl, resolvedConfigurationPath, cancellationToken);

            foreach (var feed in indexDocument.Feeds
                         .Where(entry => !string.IsNullOrWhiteSpace(entry.Value.StreamPath))
                         .OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase))
            {
                var fileName = Path.GetFileName(feed.Value.StreamPath!);
                var targetPath = Path.Combine(storageRootPath, "feeds", fileName);

                if (File.Exists(targetPath) && !forceDownload)
                {
                    skippedFeeds.Add(feed.Key);
                    _logger.LogInformation("Skipping download for {FeedName}; file already exists at {TargetPath}.", feed.Key, targetPath);
                    continue;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(targetPath) ?? storageRootPath);
                var sourceUrl = new Uri(new Uri(resolvedIndexUrl), feed.Value.StreamPath!).ToString();
                await DownloadFeedWithRetryAsync(feed.Key, sourceUrl, targetPath, cancellationToken);
                downloadedFeeds.Add(feed.Key);
            }
        }

        var loadedReplay = false;
        var startedReplay = false;

        if (File.Exists(resolvedConfigurationPath) && loadAfterDownload)
        {
            var currentStatus = _replayCoordinator.GetStatus();
            if (currentStatus.State is ReplayRunState.Running or ReplayRunState.Paused)
            {
                await _replayCoordinator.StopAsync(new StopReplayCommand(), cancellationToken);
            }

            await _replayCoordinator.LoadAsync(new LoadReplayCommand(resolvedConfigurationPath), cancellationToken);
            loadedReplay = true;
        }

        if (startAfterLoad)
        {
            await _replayCoordinator.StartAsync(new StartReplayCommand(), cancellationToken);
            startedReplay = true;
        }

        return new ReplayBootstrapResult(
            resolvedConfigurationPath,
            resolvedIndexUrl,
            downloadedFeeds.Count,
            skippedFeeds.Count,
            downloadedFeeds,
            skippedFeeds,
            loadedReplay,
            startedReplay);
    }

    private async Task<string> DownloadIndexPayloadAsync(string indexUrl, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("ReplayBootstrap");
        client.Timeout = TimeSpan.FromSeconds(Math.Max(5, _bootstrapOptions.RequestTimeoutSeconds));

        using var response = await client.GetAsync(indexUrl, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private async Task GenerateReplayConfigurationAsync(
        IndexDocument indexDocument,
        string indexUrl,
        string configurationPath,
        CancellationToken cancellationToken)
    {
        var generatedConfiguration = new ReplayConfigurationDocument
        {
            SessionId = ResolveSessionId(indexUrl),
            ReplaySpeed = _bootstrapOptions.ReplaySpeed,
            StartOffsetMs = _bootstrapOptions.StartOffsetMs,
            Feeds = indexDocument.Feeds
                .Where(entry => !string.IsNullOrWhiteSpace(entry.Value.StreamPath))
                .OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase)
                .Select(entry => new FeedDocument
                {
                    Name = entry.Key,
                    FilePath = $"./feeds/{Path.GetFileName(entry.Value.StreamPath!)}"
                })
                .ToList()
        };

        await using var stream = File.Create(configurationPath);
        await JsonSerializer.SerializeAsync(stream, generatedConfiguration, SerializerOptions, cancellationToken);
    }

    private async Task DownloadFeedWithRetryAsync(string feedName, string sourceUrl, string targetPath, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("ReplayBootstrap");
        client.Timeout = TimeSpan.FromSeconds(Math.Max(5, _bootstrapOptions.RequestTimeoutSeconds));
        var maxAttempts = Math.Max(1, _bootstrapOptions.MaxRetryAttempts);

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                _logger.LogInformation(
                    "Downloading feed {FeedName} from {SourceUrl} to {TargetPath}. Attempt {Attempt}/{MaxAttempts}.",
                    feedName,
                    sourceUrl,
                    targetPath,
                    attempt,
                    maxAttempts);

                using var response = await client.GetAsync(sourceUrl, cancellationToken);
                response.EnsureSuccessStatusCode();

                await using var sourceStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                await using var destinationStream = File.Create(targetPath);
                await sourceStream.CopyToAsync(destinationStream, cancellationToken);

                _logger.LogInformation("Downloaded feed {FeedName} to {TargetPath}.", feedName, targetPath);
                return;
            }
            catch (Exception exception) when (attempt < maxAttempts)
            {
                _logger.LogWarning(
                    exception,
                    "Download attempt {Attempt} for feed {FeedName} failed. Retrying in {RetryDelaySeconds}s.",
                    attempt,
                    feedName,
                    _bootstrapOptions.RetryDelaySeconds);

                await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, _bootstrapOptions.RetryDelaySeconds)), cancellationToken);
            }
        }

        throw new InvalidOperationException($"Feed download failed for '{feedName}' after {maxAttempts} attempts.");
    }

    private string ResolveStorageRootPath()
        => Path.IsPathRooted(_bootstrapOptions.StorageRootPath)
            ? _bootstrapOptions.StorageRootPath
            : Path.GetFullPath(Path.Combine(_hostEnvironment.ContentRootPath, _bootstrapOptions.StorageRootPath));

    private string ResolveConfigurationPath(string? configurationPath, string storageRootPath)
    {
        var candidate = string.IsNullOrWhiteSpace(configurationPath)
            ? Path.Combine(storageRootPath, _bootstrapOptions.GeneratedReplayConfigurationFileName)
            : configurationPath;

        if (string.IsNullOrWhiteSpace(candidate))
        {
            candidate = _replayServiceOptions.DefaultConfigurationPath;
        }

        if (string.IsNullOrWhiteSpace(candidate))
        {
            throw new InvalidOperationException("Replay bootstrap requires a configuration path.");
        }

        return Path.IsPathRooted(candidate)
            ? candidate
            : Path.GetFullPath(Path.Combine(_hostEnvironment.ContentRootPath, candidate));
    }

    private string ResolveIndexUrl(string? indexUrl)
    {
        var candidate = string.IsNullOrWhiteSpace(indexUrl)
            ? _bootstrapOptions.IndexUrl
            : indexUrl;

        if (string.IsNullOrWhiteSpace(candidate))
        {
            throw new InvalidOperationException("Replay bootstrap requires an Index.json URL.");
        }

        return candidate;
    }

    private string ResolveSessionId(string indexUrl)
    {
        if (!string.IsNullOrWhiteSpace(_bootstrapOptions.SessionId))
        {
            return _bootstrapOptions.SessionId;
        }

        var uri = new Uri(indexUrl);
        var segments = uri.Segments
            .Select(segment => segment.Trim('/'))
            .Where(segment => !string.IsNullOrWhiteSpace(segment) && !segment.Equals("Index.json", StringComparison.OrdinalIgnoreCase))
            .TakeLast(2)
            .Select(SanitizeSegment)
            .ToArray();

        return segments.Length == 0 ? "f1-live-timing-session" : string.Join('-', segments);
    }

    private static string SanitizeSegment(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
            }
            else if (builder.Length == 0 || builder[^1] != '-')
            {
                builder.Append('-');
            }
        }

        return builder.ToString().Trim('-');
    }

    private sealed class IndexDocument
    {
        public Dictionary<string, IndexFeedDocument> Feeds { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    private sealed class IndexFeedDocument
    {
        public string? KeyFramePath { get; set; }
        public string? StreamPath { get; set; }
    }

    private sealed class ReplayConfigurationDocument
    {
        public string SessionId { get; set; } = string.Empty;
        public double ReplaySpeed { get; set; }
        public int StartOffsetMs { get; set; }
        public List<FeedDocument> Feeds { get; set; } = [];
    }

    private sealed class FeedDocument
    {
        public string Name { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
    }
}

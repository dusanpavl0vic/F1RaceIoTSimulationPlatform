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
    private readonly IReplayCoordinator _replayCoordinator = replayCoordinator;
    private readonly ILogger<ReplayBootstrapper> _logger = logger;

    public async Task<ReplayBootstrapResult> BootstrapAsync(BootstrapReplayCommand command, CancellationToken cancellationToken)
    {
        var storageRootPath = ResolveStorageRootPath();
        Directory.CreateDirectory(storageRootPath);

        var resolvedIndexUrl = ResolveIndexUrl(command.IndexUrl, command.DownloadFeeds);
        var resolvedSessionId = ResolveSessionId(command.SessionId, resolvedIndexUrl);
        var sessionStoragePath = Path.Combine(storageRootPath, resolvedSessionId);
        Directory.CreateDirectory(sessionStoragePath);

        var resolvedConfigurationPath = ResolveConfigurationPath(command.ConfigurationPath, sessionStoragePath);
        var downloadedFeeds = new List<string>();
        var skippedFeeds = new List<string>();
        var configuredFeedCount = 0;

        if (command.DownloadFeeds)
        {
            var indexPayload = await DownloadIndexPayloadAsync(resolvedIndexUrl, cancellationToken);
            var indexPath = Path.Combine(sessionStoragePath, _bootstrapOptions.IndexFileName);
            await File.WriteAllTextAsync(indexPath, indexPayload, cancellationToken);
            await WriteLatestIndexSnapshotAsync(storageRootPath, indexPayload, cancellationToken);

            var indexDocument = JsonSerializer.Deserialize<IndexDocument>(indexPayload, SerializerOptions)
                ?? throw new InvalidOperationException("Downloaded Index.json is empty or invalid.");

            var selectedFeeds = SelectFeeds(indexDocument, command.FeedNames);
            configuredFeedCount = selectedFeeds.Count;

            await GenerateReplayConfigurationAsync(selectedFeeds, resolvedSessionId, resolvedConfigurationPath, cancellationToken);
            await WriteLatestConfigurationSnapshotAsync(storageRootPath, resolvedConfigurationPath, cancellationToken);

            foreach (var feed in selectedFeeds)
            {
                var fileName = Path.GetFileName(feed.Value.StreamPath!);
                var targetPath = Path.Combine(sessionStoragePath, "feeds", fileName);

                if (File.Exists(targetPath) && !command.ForceDownload)
                {
                    skippedFeeds.Add(feed.Key);
                    _logger.LogInformation("Skipping download for {FeedName}; file already exists at {TargetPath}.", feed.Key, targetPath);
                    continue;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(targetPath) ?? sessionStoragePath);
                var sourceUrl = new Uri(new Uri(resolvedIndexUrl), feed.Value.StreamPath!).ToString();
                await DownloadFeedWithRetryAsync(feed.Key, sourceUrl, targetPath, cancellationToken);
                downloadedFeeds.Add(feed.Key);
            }
        }
        else
        {
            configuredFeedCount = await CountConfiguredFeedsAsync(resolvedConfigurationPath, cancellationToken);
        }

        var loadedReplay = false;
        var startedReplay = false;

        if (File.Exists(resolvedConfigurationPath) && command.LoadAfterDownload)
        {
            var currentStatus = _replayCoordinator.GetStatus();
            if (currentStatus.State is ReplayRunState.Running or ReplayRunState.Paused)
            {
                await _replayCoordinator.StopAsync(new StopReplayCommand(), cancellationToken);
            }

            await _replayCoordinator.LoadAsync(new LoadReplayCommand(resolvedConfigurationPath), cancellationToken);
            loadedReplay = true;
        }

        if (command.StartAfterLoad)
        {
            await _replayCoordinator.StartAsync(new StartReplayCommand(), cancellationToken);
            startedReplay = true;
        }

        return new ReplayBootstrapResult(
            resolvedSessionId,
            sessionStoragePath,
            resolvedConfigurationPath,
            resolvedIndexUrl,
            configuredFeedCount,
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
        IReadOnlyList<KeyValuePair<string, IndexFeedDocument>> selectedFeeds,
        string sessionId,
        string configurationPath,
        CancellationToken cancellationToken)
    {
        var generatedConfiguration = new ReplayConfigurationDocument
        {
            SessionId = sessionId,
            ReplaySpeed = _bootstrapOptions.ReplaySpeed,
            StartOffsetMs = _bootstrapOptions.StartOffsetMs,
            Feeds = selectedFeeds
                .Select(entry => new FeedDocument
                {
                    Name = entry.Key,
                    FilePath = $"./feeds/{Path.GetFileName(entry.Value.StreamPath!)}"
                })
                .ToList()
        };

        Directory.CreateDirectory(Path.GetDirectoryName(configurationPath) ?? _hostEnvironment.ContentRootPath);
        await using var stream = File.Create(configurationPath);
        await JsonSerializer.SerializeAsync(stream, generatedConfiguration, SerializerOptions, cancellationToken);
    }

    private async Task WriteLatestIndexSnapshotAsync(string storageRootPath, string indexPayload, CancellationToken cancellationToken)
    {
        var latestIndexPath = Path.Combine(storageRootPath, _bootstrapOptions.IndexFileName);
        await File.WriteAllTextAsync(latestIndexPath, indexPayload, cancellationToken);
    }

    private async Task WriteLatestConfigurationSnapshotAsync(string storageRootPath, string sourceConfigurationPath, CancellationToken cancellationToken)
    {
        var latestConfigurationPath = Path.Combine(storageRootPath, _bootstrapOptions.GeneratedReplayConfigurationFileName);

        await using var source = File.OpenRead(sourceConfigurationPath);
        await using var destination = File.Create(latestConfigurationPath);
        await source.CopyToAsync(destination, cancellationToken);
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

    private string ResolveConfigurationPath(string? configurationPath, string sessionStoragePath)
    {
        var candidate = string.IsNullOrWhiteSpace(configurationPath)
            ? Path.Combine(sessionStoragePath, _bootstrapOptions.GeneratedReplayConfigurationFileName)
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

    private string ResolveIndexUrl(string? indexUrl, bool requireValue)
    {
        var candidate = string.IsNullOrWhiteSpace(indexUrl)
            ? _bootstrapOptions.IndexUrl
            : indexUrl;

        if (requireValue && string.IsNullOrWhiteSpace(candidate))
        {
            throw new InvalidOperationException("Replay bootstrap requires an Index.json URL.");
        }

        return candidate ?? string.Empty;
    }

    private string ResolveSessionId(string? requestedSessionId, string indexUrl)
    {
        if (!string.IsNullOrWhiteSpace(requestedSessionId))
        {
            return SanitizeSessionId(requestedSessionId);
        }

        if (string.IsNullOrWhiteSpace(indexUrl))
        {
            return !string.IsNullOrWhiteSpace(_bootstrapOptions.SessionId)
                ? SanitizeSessionId(_bootstrapOptions.SessionId)
                : "f1-live-timing-session";
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

    private static List<KeyValuePair<string, IndexFeedDocument>> SelectFeeds(
        IndexDocument indexDocument,
        IReadOnlyCollection<string>? requestedFeedNames)
    {
        var availableFeeds = indexDocument.Feeds
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Value.StreamPath))
            .OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (requestedFeedNames is null || requestedFeedNames.Count == 0)
        {
            return availableFeeds;
        }

        var requested = requestedFeedNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var selectedFeeds = availableFeeds
            .Where(entry => requested.Contains(entry.Key))
            .ToList();

        if (selectedFeeds.Count == 0)
        {
            throw new InvalidOperationException("None of the requested feeds were found in the supplied Index.json.");
        }

        var missingFeeds = requested
            .Where(requestedFeed => !availableFeeds.Any(entry => entry.Key.Equals(requestedFeed, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (missingFeeds.Length > 0)
        {
            throw new InvalidOperationException($"Requested feeds were not found: {string.Join(", ", missingFeeds)}.");
        }

        return selectedFeeds;
    }

    private static async Task<int> CountConfiguredFeedsAsync(string configurationPath, CancellationToken cancellationToken)
    {
        if (!File.Exists(configurationPath))
        {
            throw new FileNotFoundException("Replay configuration file was not found.", configurationPath);
        }

        await using var stream = File.OpenRead(configurationPath);
        var configuration = await JsonSerializer.DeserializeAsync<ReplayConfigurationDocument>(stream, SerializerOptions, cancellationToken)
            ?? throw new InvalidOperationException("Replay configuration file is empty or invalid.");

        return configuration.Feeds.Count;
    }

    private static string SanitizeSessionId(string value)
    {
        var sanitized = SanitizeSegment(value);
        return string.IsNullOrWhiteSpace(sanitized) ? "f1-live-timing-session" : sanitized;
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

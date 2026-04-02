using System.Text.Json;
using F1.FeedReplay.Service.Application.Contracts;
using F1.FeedReplay.Service.Domain.Models;
using Microsoft.Extensions.Options;

namespace F1.FeedReplay.Service.Infrastructure.Configuration;

public sealed class ReplayConfigurationLoader(
    IWebHostEnvironment hostEnvironment,
    IOptions<ReplayServiceOptions> replayServiceOptions) : IReplayConfigurationLoader
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IWebHostEnvironment _hostEnvironment = hostEnvironment;
    private readonly ReplayServiceOptions _replayServiceOptions = replayServiceOptions.Value;

    public async Task<ReplayConfiguration> LoadAsync(string configurationPath, CancellationToken cancellationToken)
    {
        var resolvedPath = ResolvePath(configurationPath);
        if (!File.Exists(resolvedPath))
        {
            throw new FileNotFoundException("Replay configuration file was not found.", resolvedPath);
        }

        await using var stream = File.OpenRead(resolvedPath);
        var document = await JsonSerializer.DeserializeAsync<ReplayConfigurationDocument>(stream, SerializerOptions, cancellationToken)
            ?? throw new InvalidOperationException("Replay configuration file is empty or invalid.");

        if (string.IsNullOrWhiteSpace(document.SessionId))
        {
            throw new InvalidOperationException("Replay configuration must define a sessionId.");
        }

        if (document.ReplaySpeed <= 0)
        {
            throw new InvalidOperationException("Replay speed must be greater than zero.");
        }

        if (document.Feeds.Count == 0)
        {
            throw new InvalidOperationException("Replay configuration must include at least one feed.");
        }

        var configurationDirectory = Path.GetDirectoryName(resolvedPath) ?? _hostEnvironment.ContentRootPath;
        var feeds = document.Feeds
            .Select(feed =>
            {
                if (string.IsNullOrWhiteSpace(feed.Name) || string.IsNullOrWhiteSpace(feed.FilePath))
                {
                    throw new InvalidOperationException("Each configured feed must include name and filePath.");
                }

                var filePath = Path.IsPathRooted(feed.FilePath)
                    ? feed.FilePath
                    : Path.GetFullPath(Path.Combine(configurationDirectory, feed.FilePath));

                return new FeedDefinition(feed.Name, filePath);
            })
            .ToArray();

        return new ReplayConfiguration(document.SessionId, document.ReplaySpeed, document.StartOffsetMs, feeds);
    }

    private string ResolvePath(string configurationPath)
    {
        var candidate = string.IsNullOrWhiteSpace(configurationPath)
            ? _replayServiceOptions.DefaultConfigurationPath
            : configurationPath;

        if (string.IsNullOrWhiteSpace(candidate))
        {
            throw new InvalidOperationException("A replay configuration path must be provided.");
        }

        return Path.IsPathRooted(candidate)
            ? candidate
            : Path.GetFullPath(Path.Combine(_hostEnvironment.ContentRootPath, candidate));
    }

    private sealed class ReplayConfigurationDocument
    {
        public string SessionId { get; set; } = string.Empty;
        public double ReplaySpeed { get; set; } = 1.0d;
        public int StartOffsetMs { get; set; }
        public List<FeedDocument> Feeds { get; set; } = [];
    }

    private sealed class FeedDocument
    {
        public string Name { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
    }
}

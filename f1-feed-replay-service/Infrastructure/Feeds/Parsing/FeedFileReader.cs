using System.Text.Json;
using System.Text.Json.Nodes;

namespace F1.FeedReplay.Service.Infrastructure.Feeds.Parsing;

public sealed class FeedFileReader(ILogger<FeedFileReader> logger)
{
    private readonly ILogger<FeedFileReader> _logger = logger;

    public async Task<IReadOnlyList<FeedEntry>> ReadEntriesAsync(string filePath, CancellationToken cancellationToken)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Feed file was not found.", filePath);
        }

        var rawContent = await File.ReadAllTextAsync(filePath, cancellationToken);
        if (string.IsNullOrWhiteSpace(rawContent))
        {
            return Array.Empty<FeedEntry>();
        }

        var trimmed = rawContent.Trim();
        if (trimmed.StartsWith("[", StringComparison.Ordinal))
        {
            var array = JsonNode.Parse(trimmed)?.AsArray() ?? [];
            return array
                .Where(node => node is not null)
                .Select(node => new FeedEntry(null, node!.DeepClone()))
                .ToArray();
        }

        if ((trimmed.StartsWith("{", StringComparison.Ordinal) || trimmed.StartsWith("\"", StringComparison.Ordinal))
            && !trimmed.Contains(Environment.NewLine, StringComparison.Ordinal))
        {
            var singleObject = JsonNode.Parse(trimmed);
            return singleObject is null ? Array.Empty<FeedEntry>() : [new FeedEntry(null, singleObject)];
        }

        var entries = new List<FeedEntry>();
        using var reader = new StringReader(rawContent);
        while (reader.ReadLine() is { } line)
        {
            var normalizedLine = line.TrimStart('\uFEFF');
            if (string.IsNullOrWhiteSpace(normalizedLine))
            {
                continue;
            }

            var timestampToken = TryExtractTimestampToken(normalizedLine, out var jsonPayload) ? normalizedLine[..12] : null;
            try
            {
                var node = JsonNode.Parse(jsonPayload);
                if (node is not null)
                {
                    entries.Add(new FeedEntry(timestampToken, node));
                }
            }
            catch (JsonException exception)
            {
                _logger.LogWarning(
                    exception,
                    "Skipping malformed line while reading feed file {FilePath}.",
                    filePath);
            }
        }

        return entries;
    }

    private static bool TryExtractTimestampToken(string line, out string jsonPayload)
    {
        if (line.Length > 12
            && line[2] == ':'
            && line[5] == ':'
            && line[8] == '.'
            && (line[12] == '{' || line[12] == '[' || line[12] == '"'))
        {
            jsonPayload = line[12..];
            return true;
        }

        jsonPayload = line;
        return false;
    }
}

public sealed record FeedEntry(string? TimestampToken, JsonNode Payload);

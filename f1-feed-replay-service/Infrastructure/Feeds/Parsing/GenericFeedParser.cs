using System.Globalization;
using System.Text.Json.Nodes;
using F1.FeedReplay.Service.Application.Contracts;
using F1.FeedReplay.Service.Domain.Models;
using F1.FeedReplay.Service.Domain.Services;

namespace F1.FeedReplay.Service.Infrastructure.Feeds.Parsing;

public class GenericFeedParser(FeedFileReader fileReader, ILogger<GenericFeedParser> logger) : IFeedParser
{
    private readonly FeedFileReader _fileReader = fileReader;
    private readonly ILogger<GenericFeedParser> _logger = logger;

    public virtual async Task<IReadOnlyList<ReplayEvent>> ParseAsync(
        ReplayConfiguration configuration,
        FeedDefinition feedDefinition,
        int feedOrder,
        CancellationToken cancellationToken)
    {
        var entries = await _fileReader.ReadEntriesAsync(feedDefinition.FilePath, cancellationToken);
        var events = new List<ReplayEvent>(entries.Count);

        for (var entryIndex = 0; entryIndex < entries.Count; entryIndex++)
        {
            var entry = entries[entryIndex];
            try
            {
                var eventTime = ResolveEventTime(entry, configuration);
                var driverNumber = ResolveDriverNumber(entry.Payload);
                var sequence = ResolveSequence(entry.Payload, feedOrder, entryIndex);
                var deviceId = DeviceIdentity.Resolve(feedDefinition.Name, driverNumber);

                events.Add(new ReplayEvent(
                    configuration.SessionId,
                    feedDefinition.Name,
                    deviceId,
                    eventTime,
                    sequence,
                    feedOrder * 1_000_000 + entryIndex,
                    driverNumber,
                    entry.Payload.DeepClone()));
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Skipping malformed entry {EntryIndex} from feed {FeedName}.",
                    entryIndex,
                    feedDefinition.Name);
            }
        }

        _logger.LogInformation(
            "Parsed {EventCount} events from feed {FeedName} ({FilePath}).",
            events.Count,
            feedDefinition.Name,
            feedDefinition.FilePath);

        return events;
    }

    protected virtual DateTimeOffset ResolveEventTime(FeedEntry entry, ReplayConfiguration configuration)
    {
        if (!string.IsNullOrWhiteSpace(entry.TimestampToken)
            && TimeSpan.TryParseExact(entry.TimestampToken, @"hh\:mm\:ss\.fff", CultureInfo.InvariantCulture, out var relativeTime))
        {
            return ResolveSessionBaseTime(configuration) + relativeTime;
        }

        foreach (var candidate in new[]
                 {
                     "eventTime",
                     "EventTime",
                     "timestamp",
                     "Timestamp",
                     "Utc",
                     "utc",
                     "Time",
                     "time",
                     "Date",
                     "date"
                 })
        {
            if (TryGetNodeValue(entry.Payload, candidate, out var value) && TryParseDate(value, out var parsed))
            {
                return parsed;
            }
        }

        throw new InvalidOperationException("Event timestamp was not found in the feed entry.");
    }

    protected virtual int? ResolveDriverNumber(JsonNode entry)
    {
        foreach (var candidate in new[]
                 {
                     "driverNumber",
                     "DriverNumber",
                     "RacingNumber",
                     "racingNumber",
                     "driverNo",
                     "DriverNo",
                     "Number"
                 })
        {
            if (TryGetNodeValue(entry, candidate, out var value))
            {
                if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                {
                    return parsed;
                }
            }
        }

        return null;
    }

    protected virtual long ResolveSequence(JsonNode entry, int feedOrder, int entryIndex)
    {
        foreach (var candidate in new[] { "sequence", "Sequence", "seq", "Seq" })
        {
            if (TryGetNodeValue(entry, candidate, out var value)
                && long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            {
                return parsed;
            }
        }

        return ((long)feedOrder + 1) * 1_000_000L + entryIndex;
    }

    protected static bool TryGetNodeValue(JsonNode? node, string propertyName, out string value)
    {
        if (node is JsonObject jsonObject)
        {
            foreach (var property in jsonObject)
            {
                if (string.Equals(property.Key, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    if (property.Value is not null)
                    {
                        value = property.Value.ToString();
                        return true;
                    }
                }

                if (property.Value is not null && TryGetNodeValue(property.Value, propertyName, out value))
                {
                    return true;
                }
            }
        }

        if (node is JsonArray jsonArray)
        {
            foreach (var child in jsonArray)
            {
                if (child is not null && TryGetNodeValue(child, propertyName, out value))
                {
                    return true;
                }
            }
        }

        value = string.Empty;
        return false;
    }

    private static bool TryParseDate(string value, out DateTimeOffset result)
    {
        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out result))
        {
            return true;
        }

        if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var unixMilliseconds))
        {
            result = DateTimeOffset.FromUnixTimeMilliseconds(unixMilliseconds);
            return true;
        }

        result = default;
        return false;
    }

    private static DateTimeOffset ResolveSessionBaseTime(ReplayConfiguration configuration)
    {
        if (configuration.SessionId.Length >= 10
            && DateTimeOffset.TryParseExact(
                configuration.SessionId[..10],
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal,
                out var parsed))
        {
            return parsed;
        }

        return DateTimeOffset.UnixEpoch;
    }
}

using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using F1.EventNormalizer.Service.Application.Contracts;
using F1.EventNormalizer.Service.Application.Models;
using F1.EventNormalizer.Service.Infrastructure.Feeds;
using F1.Shared.Models;

namespace F1.EventNormalizer.Service.Application.Services;

public sealed class CanonicalEventFactory(IPositionCoordinateResolver positionCoordinateResolver) : ICanonicalEventFactory
{
    private static readonly HashSet<string> IgnoredFeeds = new(StringComparer.Ordinal)
    {
        "WeatherData",
        "ChampionshipPrediction",
        "ContentStreams",
        "ExtrapolatedClock",
        "Heartbeat",
        "TeamRadio",
        "WeatherDataSeries",
        "PitLaneTimeCollection",
        "Position.z",
        "CarData.z"
    };

    private readonly IPositionCoordinateResolver _positionCoordinateResolver = positionCoordinateResolver;

    public IReadOnlyList<CanonicalEvent> Create(RawReplayEvent rawEvent)
    {
        if (IgnoredFeeds.Contains(rawEvent.SourceFeed))
        {
            return [];
        }

        var rawData = rawEvent.Payload["rawData"];
        if (rawData is null)
        {
            return [BuildCanonicalEvent(rawEvent, MapEventType(rawEvent.SourceFeed), rawEvent.DriverNumber, new JsonObject())];
        }

        return rawEvent.SourceFeed switch
        {
            "TimingData" => CreateLineEvents(rawEvent, rawData["Lines"], "timing.driver.updated", "timing"),
            "TimingStats" => CreateLineEvents(rawEvent, rawData["Lines"], "timing.stats.updated", "timingStats"),
            "TimingAppData" => CreateLineEvents(rawEvent, rawData["Lines"], "timing.app.updated", "timingApp"),
            "DriverList" when rawData is JsonObject driverList => CreateDriverListEvents(rawEvent, driverList),
            "CurrentTyres" => CreateTyreEvents(rawEvent, rawData["Tyres"], "tyres.current.updated", "tyres"),
            "TyreStintSeries" => CreateTyreEvents(rawEvent, rawData["Stints"], "tyres.stint.updated", "stints"),
            "PitLaneTimeCollection" => CreatePitLaneEvents(rawEvent, rawData["PitTimes"]),
            "Position.z" => CreateDecodedPositionEvents(rawEvent, rawData),
            "CarData.z" => CreateDecodedTelemetryEvents(rawEvent, rawData),
            _ => [BuildCanonicalEvent(rawEvent, MapEventType(rawEvent.SourceFeed), rawEvent.DriverNumber, WrapPayload(rawData.DeepClone()))]
        };
    }

    private IReadOnlyList<CanonicalEvent> CreateLineEvents(
        RawReplayEvent rawEvent,
        JsonNode? linesNode,
        string eventType,
        string payloadProperty)
    {
        if (linesNode is not JsonObject lines)
        {
            return [BuildCanonicalEvent(rawEvent, eventType, rawEvent.DriverNumber, WrapPayload(linesNode?.DeepClone()))];
        }

        var order = 0;
        return lines.Select(line => CreateLineEvent(rawEvent, line.Key, line.Value?.DeepClone(), eventType, payloadProperty, ++order))
            .Where(evt => evt is not null)
            .Cast<CanonicalEvent>()
            .ToArray();
    }

    private CanonicalEvent? CreateLineEvent(
        RawReplayEvent rawEvent,
        string driverKey,
        JsonNode? linePayload,
        string eventType,
        string payloadProperty,
        int order)
    {
        if (linePayload is null)
        {
            return null;
        }

        var driverNumber = TryParseDriverNumber(driverKey)
            ?? TryParseDriverNumber(linePayload["RacingNumber"]?.ToString())
            ?? rawEvent.DriverNumber;

        return BuildCanonicalEvent(
            rawEvent,
            eventType,
            driverNumber,
            new JsonObject
            {
                ["driverNumber"] = driverNumber,
                [payloadProperty] = linePayload
            },
            sequenceOverride: CreateDerivedSequence(rawEvent.Sequence, order));
    }

    private IReadOnlyList<CanonicalEvent> CreateDriverListEvents(RawReplayEvent rawEvent, JsonObject driverList)
    {
        var order = 0;
        return driverList.Select(entry => CreateDriverListEvent(rawEvent, entry.Key, entry.Value?.DeepClone(), ++order))
            .Where(evt => evt is not null)
            .Cast<CanonicalEvent>()
            .ToArray();
    }

    private CanonicalEvent? CreateDriverListEvent(RawReplayEvent rawEvent, string driverKey, JsonNode? driverPayload, int order)
    {
        if (driverPayload is null)
        {
            return null;
        }

        var driverNumber = TryParseDriverNumber(driverKey)
            ?? TryParseDriverNumber(driverPayload["RacingNumber"]?.ToString())
            ?? rawEvent.DriverNumber;

        return BuildCanonicalEvent(
            rawEvent,
            "driver.list.updated",
            driverNumber,
            new JsonObject
            {
                ["driverNumber"] = driverNumber,
                ["driver"] = driverPayload
            },
            sequenceOverride: CreateDerivedSequence(rawEvent.Sequence, order));
    }

    private IReadOnlyList<CanonicalEvent> CreateTyreEvents(RawReplayEvent rawEvent, JsonNode? collectionNode, string eventType, string payloadProperty)
    {
        if (collectionNode is not JsonObject collection)
        {
            return [BuildCanonicalEvent(rawEvent, eventType, rawEvent.DriverNumber, WrapPayload(collectionNode?.DeepClone()))];
        }

        var events = new List<CanonicalEvent>();
        var order = 0;
        foreach (var entry in collection)
        {
            var driverNumber = TryParseDriverNumber(entry.Key)
                ?? TryParseDriverNumber(entry.Value?["RacingNumber"]?.ToString())
                ?? TryParseDriverNumber(entry.Value?["driverNumber"]?.ToString());

            if (driverNumber is null || entry.Value is null)
            {
                continue;
            }

            events.Add(BuildCanonicalEvent(
                rawEvent,
                eventType,
                driverNumber,
                new JsonObject
                {
                    ["driverNumber"] = driverNumber,
                    [payloadProperty] = entry.Value.DeepClone()
                },
                sequenceOverride: CreateDerivedSequence(rawEvent.Sequence, ++order)));
        }

        return events.Count > 0
            ? events
            : [BuildCanonicalEvent(rawEvent, eventType, rawEvent.DriverNumber, WrapPayload(collectionNode.DeepClone()))];
    }

    private IReadOnlyList<CanonicalEvent> CreatePitLaneEvents(RawReplayEvent rawEvent, JsonNode? pitTimesNode)
    {
        if (pitTimesNode is not JsonObject pitTimes)
        {
            return [BuildCanonicalEvent(rawEvent, "pitlane.time.updated", rawEvent.DriverNumber, WrapPayload(pitTimesNode?.DeepClone()))];
        }

        var events = new List<CanonicalEvent>();
        var order = 0;
        foreach (var entry in pitTimes)
        {
            var driverNumber = TryParseDriverNumber(entry.Key)
                ?? TryParseDriverNumber(entry.Value?["RacingNumber"]?.ToString());

            if (driverNumber is null || entry.Value is null)
            {
                continue;
            }

            events.Add(BuildCanonicalEvent(
                rawEvent,
                "pitlane.time.updated",
                driverNumber,
                new JsonObject
                {
                    ["driverNumber"] = driverNumber,
                    ["pit"] = entry.Value.DeepClone()
                },
                sequenceOverride: CreateDerivedSequence(rawEvent.Sequence, ++order)));
        }

        return events.Count > 0
            ? events
            : [BuildCanonicalEvent(rawEvent, "pitlane.time.updated", rawEvent.DriverNumber, WrapPayload(pitTimes.DeepClone()))];
    }

    private IReadOnlyList<CanonicalEvent> CreateDecodedPositionEvents(RawReplayEvent rawEvent, JsonNode rawData)
    {
        if (CompressedFeedDecoder.DecodeRawData(rawData) is not JsonObject decoded
            || decoded["Position"] is not JsonArray positions)
        {
            return [BuildCanonicalEvent(rawEvent, "car.position.updated", rawEvent.DriverNumber, WrapPayload(rawData.DeepClone()))];
        }

        var events = new List<CanonicalEvent>();
        var order = 0;
        foreach (var packet in positions.OfType<JsonObject>())
        {
            var timestamp = packet["Timestamp"]?.ToString();
            var eventTime = TryParseDate(timestamp, out var parsedTimestamp) ? parsedTimestamp : rawEvent.EventTime;
            if (packet["Entries"] is not JsonObject entries)
            {
                continue;
            }

            foreach (var entry in entries)
            {
                var driverNumber = TryParseDriverNumber(entry.Key);
                if (driverNumber is null || entry.Value is not JsonObject position)
                {
                    continue;
                }

                var resolvedCoordinates = ResolvePositionCoordinates(rawEvent, driverNumber.Value, position);
                events.Add(BuildCanonicalEvent(
                    rawEvent,
                    "car.position.updated",
                    driverNumber,
                    new JsonObject
                    {
                        ["driverNumber"] = driverNumber,
                        ["position"] = new JsonObject
                        {
                            ["timestamp"] = timestamp,
                            ["status"] = position["Status"]?.ToString(),
                            ["x"] = resolvedCoordinates.X,
                            ["y"] = resolvedCoordinates.Y,
                            ["z"] = resolvedCoordinates.Z,
                            ["rawX"] = resolvedCoordinates.RawX,
                            ["rawY"] = resolvedCoordinates.RawY,
                            ["rawZ"] = resolvedCoordinates.RawZ,
                            ["hasRawCoordinates"] = resolvedCoordinates.HasRawCoordinates,
                            ["isEstimated"] = resolvedCoordinates.IsEstimated
                        }
                    },
                    eventTimeOverride: eventTime,
                    sequenceOverride: CreateDerivedSequence(rawEvent.Sequence, ++order)));
            }
        }

        return events.Count > 0
            ? events
            : [BuildCanonicalEvent(rawEvent, "car.position.updated", rawEvent.DriverNumber, WrapPayload(decoded.DeepClone()))];
    }

    private IReadOnlyList<CanonicalEvent> CreateDecodedTelemetryEvents(RawReplayEvent rawEvent, JsonNode rawData)
    {
        if (CompressedFeedDecoder.DecodeRawData(rawData) is not JsonObject decoded
            || decoded["Entries"] is not JsonArray entriesArray)
        {
            return [BuildCanonicalEvent(rawEvent, "car.telemetry.updated", rawEvent.DriverNumber, WrapPayload(rawData.DeepClone()))];
        }

        var events = new List<CanonicalEvent>();
        var order = 0;
        foreach (var packet in entriesArray.OfType<JsonObject>())
        {
            var utc = packet["Utc"]?.ToString();
            var eventTime = TryParseDate(utc, out var parsedUtc) ? parsedUtc : rawEvent.EventTime;
            if (packet["Cars"] is not JsonObject cars)
            {
                continue;
            }

            foreach (var carEntry in cars)
            {
                var driverNumber = TryParseDriverNumber(carEntry.Key)
                    ?? TryParseDriverNumber(carEntry.Value?["RacingNumber"]?.ToString());

                if (driverNumber is null || carEntry.Value is not JsonObject car)
                {
                    continue;
                }

                var channels = car["Channels"] as JsonObject;
                var rpm = TryGetInteger(channels?["0"]);
                var speed = TryGetInteger(channels?["2"]);
                var gear = TryGetInteger(channels?["3"]);
                var rawThrottle = TryGetInteger(channels?["4"]);
                var rawBrake = TryGetInteger(channels?["5"]);
                var drs = TryGetInteger(channels?["45"]);

                events.Add(BuildCanonicalEvent(
                    rawEvent,
                    "car.telemetry.updated",
                    driverNumber,
                    new JsonObject
                    {
                        ["driverNumber"] = driverNumber,
                        ["telemetry"] = new JsonObject
                        {
                            ["utc"] = utc,
                            ["rpm"] = rpm,
                            ["speed"] = speed,
                            ["speedKph"] = speed,
                            ["gear"] = gear,
                            ["throttle"] = NormalizeThrottle(rawThrottle),
                            ["throttlePct"] = NormalizeThrottle(rawThrottle),
                            ["rawThrottle"] = rawThrottle,
                            ["brake"] = NormalizeBrake(rawBrake),
                            ["brakeApplied"] = NormalizeBrake(rawBrake),
                            ["rawBrake"] = rawBrake,
                            ["drs"] = drs,
                            ["drsEnabled"] = IsDrsEnabled(drs),
                            ["drsAvailable"] = IsDrsAvailable(drs),
                            ["channels"] = channels?.DeepClone()
                        }
                    },
                    eventTimeOverride: eventTime,
                    sequenceOverride: CreateDerivedSequence(rawEvent.Sequence, ++order)));
            }
        }

        return events.Count > 0
            ? events
            : [BuildCanonicalEvent(rawEvent, "car.telemetry.updated", rawEvent.DriverNumber, WrapPayload(decoded.DeepClone()))];
    }

    private static CanonicalEvent BuildCanonicalEvent(
        RawReplayEvent rawEvent,
        string eventType,
        int? driverNumber,
        JsonObject payload,
        DateTimeOffset? eventTimeOverride = null,
        long? sequenceOverride = null)
        => new(
            Guid.NewGuid().ToString("D"),
            rawEvent.SessionId,
            eventType,
            eventTimeOverride ?? rawEvent.EventTime,
            rawEvent.PublishTime,
            sequenceOverride ?? rawEvent.Sequence,
            new CanonicalSource(rawEvent.SourceFeed, rawEvent.DeviceId),
            driverNumber,
            payload);

    private static JsonObject WrapPayload(JsonNode? payload)
        => new()
        {
            ["data"] = payload
        };

    private static string MapEventType(string sourceFeed)
        => sourceFeed switch
        {
            "SessionInfo" => "session.info.updated",
            "TrackStatus" => "track.status.updated",
            "TimingData" => "timing.driver.updated",
            "TimingStats" => "timing.stats.updated",
            "TimingAppData" => "timing.app.updated",
            "LapCount" => "lap.count.updated",
            "RaceControlMessages" => "race-control.message",
            "DriverList" => "driver.list.updated",
            "CarData.z" => "car.telemetry.updated",
            "Position.z" => "car.position.updated",
            "CurrentTyres" => "tyres.current.updated",
            "TyreStintSeries" => "tyres.stint.updated",
            "PitLaneTimeCollection" => "pitlane.time.updated",
            _ => $"feed.{ToKebabCase(sourceFeed)}.updated"
        };

    private static int? TryParseDriverNumber(string? value)
        => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;

    private static bool TryParseDate(string? value, out DateTimeOffset result)
        => DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out result);

    private static long CreateDerivedSequence(long baseSequence, int localOrder)
        => checked(baseSequence * 1_000L + localOrder);

    private static string ToKebabCase(string input)
    {
        var builder = new StringBuilder(input.Length + 8);
        for (var index = 0; index < input.Length; index++)
        {
            var character = input[index];
            if (char.IsUpper(character) && index > 0)
            {
                builder.Append('-');
            }

            builder.Append(character is '.' or '_' ? '-' : char.ToLowerInvariant(character));
        }

        return builder.ToString();
    }

    private ResolvedPositionCoordinates ResolvePositionCoordinates(RawReplayEvent rawEvent, int driverNumber, JsonObject position)
        => _positionCoordinateResolver.Resolve(
            rawEvent.SessionId,
            driverNumber,
            TryGetInteger(position["X"]),
            TryGetInteger(position["Y"]),
            TryGetInteger(position["Z"]));

    private static int? TryGetInteger(JsonNode? node)
    {
        if (node is null)
        {
            return null;
        }

        if (node is JsonValue jsonValue && jsonValue.TryGetValue<int>(out var asInt))
        {
            return asInt;
        }

        return int.TryParse(node.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private static int? NormalizeThrottle(int? rawThrottle)
    {
        if (rawThrottle is null)
        {
            return null;
        }

        var normalized = (int)Math.Round(rawThrottle.Value / 104d * 100d, MidpointRounding.AwayFromZero);
        return Math.Clamp(normalized, 0, 100);
    }

    private static bool? NormalizeBrake(int? rawBrake)
        => rawBrake is null ? null : rawBrake.Value > 0;

    private static bool? IsDrsEnabled(int? drs)
        => drs is null ? null : drs.Value is 10 or 12 or 14;

    private static bool? IsDrsAvailable(int? drs)
        => drs is null ? null : drs.Value is 8 or 10 or 12 or 14;
}

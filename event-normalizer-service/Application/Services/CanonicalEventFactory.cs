using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.Json.Nodes;
using F1.EventNormalizer.Service.Application.Contracts;
using F1.Shared.Models;

namespace F1.EventNormalizer.Service.Application.Services;

public sealed class CanonicalEventFactory : ICanonicalEventFactory
{
    public IReadOnlyList<CanonicalEvent> Create(RawReplayEvent rawEvent)
    {
        var rawData = rawEvent.Payload["rawData"];
        if (rawData is null)
        {
            return [BuildCanonicalEvent(rawEvent, MapEventType(rawEvent.SourceFeed), rawEvent.DriverNumber, new JsonObject())];
        }

        return rawEvent.SourceFeed switch
        {
            "TimingData" => CreateLineEvents(rawEvent, rawData["Lines"], "timing.driver.updated", "timing"),
            "TimingAppData" => CreateLineEvents(rawEvent, rawData["Lines"], "timing.app.updated", "timingApp"),
            "DriverList" when rawData is JsonObject driverList => CreateDriverListEvents(rawEvent, driverList),
            "CurrentTyres" => CreateTyreEvents(rawEvent, rawData["Tyres"], "tyres.current.updated", "tyres"),
            "TyreStintSeries" => CreateTyreEvents(rawEvent, rawData["Stints"], "tyres.stint.updated", "stints"),
            "PitLaneTimeCollection" => CreatePitLaneEvents(rawEvent, rawData["PitTimes"]),
            "TeamRadio" => CreateTeamRadioEvents(rawEvent, rawData["Captures"]),
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

    private IReadOnlyList<CanonicalEvent> CreateTeamRadioEvents(RawReplayEvent rawEvent, JsonNode? capturesNode)
    {
        var captures = capturesNode switch
        {
            JsonArray array => array.Where(item => item is JsonObject).Cast<JsonObject>().ToArray(),
            JsonObject objectCaptures => objectCaptures.Select(pair => pair.Value as JsonObject).Where(item => item is not null).Cast<JsonObject>().ToArray(),
            _ => []
        };

        if (captures.Length == 0)
        {
            return [BuildCanonicalEvent(rawEvent, "team-radio.capture", rawEvent.DriverNumber, WrapPayload(capturesNode?.DeepClone()))];
        }

        var order = 0;
        return captures
            .Select(capture =>
            {
                var driverNumber = TryParseDriverNumber(capture["RacingNumber"]?.ToString()) ?? rawEvent.DriverNumber;
                return BuildCanonicalEvent(
                    rawEvent,
                    "team-radio.capture",
                    driverNumber,
                    new JsonObject
                    {
                        ["driverNumber"] = driverNumber,
                        ["radio"] = new JsonObject
                        {
                            ["driverNumber"] = driverNumber,
                            ["utc"] = capture["Utc"]?.ToString(),
                            ["path"] = capture["Path"]?.ToString()
                        }
                    },
                    eventTimeOverride: TryParseDate(capture["Utc"]?.ToString(), out var parsedUtc) ? parsedUtc : rawEvent.EventTime,
                    sequenceOverride: CreateDerivedSequence(rawEvent.Sequence, ++order));
            })
            .ToArray();
    }

    private IReadOnlyList<CanonicalEvent> CreateDecodedPositionEvents(RawReplayEvent rawEvent, JsonNode rawData)
    {
        if (TryDecodeCompressedJson(rawData) is not JsonObject decoded
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
                            ["x"] = position["X"]?.DeepClone(),
                            ["y"] = position["Y"]?.DeepClone(),
                            ["z"] = position["Z"]?.DeepClone()
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
        if (TryDecodeCompressedJson(rawData) is not JsonObject decoded
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
                            ["rpm"] = channels?["0"]?.DeepClone(),
                            ["speed"] = channels?["2"]?.DeepClone(),
                            ["gear"] = channels?["3"]?.DeepClone(),
                            ["throttle"] = channels?["4"]?.DeepClone(),
                            ["brake"] = channels?["5"]?.DeepClone(),
                            ["drs"] = channels?["45"]?.DeepClone(),
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

    private static JsonObject? TryDecodeCompressedJson(JsonNode rawData)
    {
        string encoded;
        try
        {
            encoded = rawData.GetValue<string>();
        }
        catch
        {
            return rawData as JsonObject;
        }

        try
        {
            var compressedBytes = Convert.FromBase64String(encoded);
            using var input = new MemoryStream(compressedBytes);
            using var deflateStream = new DeflateStream(input, CompressionMode.Decompress);
            using var reader = new StreamReader(deflateStream, Encoding.UTF8);
            var json = reader.ReadToEnd();
            return JsonNode.Parse(json) as JsonObject;
        }
        catch
        {
            return null;
        }
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
            "TimingAppData" => "timing.app.updated",
            "LapCount" => "lap.count.updated",
            "WeatherData" => "weather.updated",
            "RaceControlMessages" => "race-control.message",
            "DriverList" => "driver.list.updated",
            "CarData.z" => "car.telemetry.updated",
            "Position.z" => "car.position.updated",
            "CurrentTyres" => "tyres.current.updated",
            "TyreStintSeries" => "tyres.stint.updated",
            "PitLaneTimeCollection" => "pitlane.time.updated",
            "TeamRadio" => "team-radio.capture",
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
}

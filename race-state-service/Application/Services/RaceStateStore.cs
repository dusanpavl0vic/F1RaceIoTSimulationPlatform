using System.Text.Json.Nodes;
using F1.RaceState.Service.Application.Contracts;
using F1.RaceState.Service.Application.Models;
using F1.RaceState.Service.Domain.Models;
using F1.Shared.Models;

namespace F1.RaceState.Service.Application.Services;

public sealed class RaceStateStore : IRaceStateStore
{
    private readonly object _gate = new();
    private RaceStateSnapshot _snapshot = new();
    private Dictionary<string, AppliedEventVersion> _lastAppliedEventVersions = new(StringComparer.Ordinal);

    public RaceStateApplyResult Apply(CanonicalEvent canonicalEvent)
    {
        lock (_gate)
        {
            _snapshot.SessionId ??= canonicalEvent.SessionId;
            var stateKey = ResolveStateKey(canonicalEvent);
            if (stateKey is null)
            {
                return new RaceStateApplyResult(CloneSnapshot(_snapshot), false, "Event type does not mutate the current race state.", null);
            }

            var candidateVersion = new AppliedEventVersion(canonicalEvent.EventTime, canonicalEvent.Sequence);
            if (IsLateEventForStateKey(stateKey, candidateVersion, out var currentVersion))
            {
                return new RaceStateApplyResult(
                    CloneSnapshot(_snapshot),
                    false,
                    $"Late event ignored for key '{stateKey}'. Incoming=({candidateVersion.EventTime:o}, {candidateVersion.Sequence}) Current=({currentVersion!.EventTime:o}, {currentVersion.Sequence}).",
                    stateKey);
            }

            var applied = canonicalEvent.EventType switch
            {
                "session.info.updated" => ApplySessionInfo(canonicalEvent),
                "track.status.updated" => ApplyTrackStatus(canonicalEvent),
                "lap.count.updated" => ApplyLapCount(canonicalEvent),
                "driver.list.updated" => ApplyDriverMetadata(canonicalEvent),
                "timing.driver.updated" => ApplyTiming(canonicalEvent),
                "timing.stats.updated" => ApplyTimingStats(canonicalEvent),
                "timing.app.updated" => ApplyTimingApp(canonicalEvent),
                "tyres.current.updated" => ApplyCurrentTyres(canonicalEvent),
                "tyres.stint.updated" => ApplyTyreStints(canonicalEvent),
                "pitlane.time.updated" => ApplyPitLaneTime(canonicalEvent),
                "car.position.updated" => ApplyPosition(canonicalEvent),
                "car.telemetry.updated" => ApplyTelemetry(canonicalEvent),
                _ => false
            };

            if (!applied)
            {
                return new RaceStateApplyResult(CloneSnapshot(_snapshot), false, "Event type is unsupported for race state mutation.", stateKey);
            }

            StoreCanonicalEventState(canonicalEvent);
            _snapshot.UpdatedAt = DateTimeOffset.UtcNow;
            _snapshot.LastProcessedEventTime = canonicalEvent.EventTime;
            _snapshot.LastProcessedSequence = canonicalEvent.Sequence;
            _lastAppliedEventVersions[stateKey] = candidateVersion;
            return new RaceStateApplyResult(CloneSnapshot(_snapshot), true, null, stateKey);
        }
    }

    public RaceStateSnapshot GetSnapshot()
    {
        lock (_gate)
        {
            return CloneSnapshot(_snapshot);
        }
    }

    public RaceStateCheckpoint GetCheckpoint()
    {
        lock (_gate)
        {
            return new RaceStateCheckpoint
            {
                Snapshot = CloneSnapshot(_snapshot),
                LastAppliedEventVersions = _lastAppliedEventVersions.ToDictionary(
                    entry => entry.Key,
                    entry => new AppliedEventVersion(entry.Value.EventTime, entry.Value.Sequence),
                    StringComparer.Ordinal)
            };
        }
    }

    public void Restore(RaceStateCheckpoint checkpoint)
    {
        ArgumentNullException.ThrowIfNull(checkpoint);

        lock (_gate)
        {
            _snapshot = CloneSnapshot(checkpoint.Snapshot);
            _lastAppliedEventVersions = checkpoint.LastAppliedEventVersions.ToDictionary(
                entry => entry.Key,
                entry => new AppliedEventVersion(entry.Value.EventTime, entry.Value.Sequence),
                StringComparer.Ordinal);
        }
    }

    private bool IsLateEventForStateKey(string stateKey, AppliedEventVersion candidateVersion, out AppliedEventVersion? currentVersion)
    {
        if (_lastAppliedEventVersions.TryGetValue(stateKey, out currentVersion)
            && candidateVersion.CompareTo(currentVersion) <= 0)
        {
            return true;
        }

        currentVersion = null;
        return false;
    }

    private bool ApplySessionInfo(CanonicalEvent canonicalEvent)
    {
        _snapshot.SessionInfo = canonicalEvent.Payload["data"]?.AsObject()?.DeepClone().AsObject();
        return true;
    }

    private bool ApplyTrackStatus(CanonicalEvent canonicalEvent)
    {
        var payload = canonicalEvent.Payload["data"];
        _snapshot.TrackStatusCode = payload?["Status"]?.ToString();
        _snapshot.TrackStatusMessage = payload?["Message"]?.ToString();
        return true;
    }

    private bool ApplyLapCount(CanonicalEvent canonicalEvent)
    {
        var payload = canonicalEvent.Payload["data"];
        _snapshot.CurrentLap = FirstNonNull(TryParseInt(payload?["CurrentLap"]?.ToString()), _snapshot.CurrentLap);
        _snapshot.TotalLaps = FirstNonNull(_snapshot.TotalLaps, TryParseInt(payload?["TotalLaps"]?.ToString()))
            ?? TryParseInt(payload?["TotalLaps"]?.ToString());
        return true;
    }

    private bool ApplyDriverMetadata(CanonicalEvent canonicalEvent)
    {
        if (canonicalEvent.DriverNumber is not int driverNumber)
        {
            return false;
        }

        var driver = GetOrCreateDriver(driverNumber);
        var payload = canonicalEvent.Payload["driver"];
        driver.BroadcastName = FirstNonEmpty(payload?["BroadcastName"]?.ToString(), driver.BroadcastName);
        driver.FullName = FirstNonEmpty(
            payload?["FullName"]?.ToString()
                ?? BuildFullName(payload?["FirstName"]?.ToString(), payload?["LastName"]?.ToString())
                ?? payload?["Tla"]?.ToString(),
            driver.FullName);
        driver.Tla = FirstNonEmpty(payload?["Tla"]?.ToString(), driver.Tla);
        driver.TeamName = FirstNonEmpty(payload?["TeamName"]?.ToString(), driver.TeamName);
        driver.TeamColor = FirstNonEmpty(NormalizeColor(payload?["TeamColour"]?.ToString()), driver.TeamColor);
        driver.Line = FirstNonNull(TryParseInt(payload?["Line"]?.ToString()), driver.Line);
        return true;
    }

    private bool ApplyTiming(CanonicalEvent canonicalEvent)
    {
        if (canonicalEvent.DriverNumber is not int driverNumber)
        {
            return false;
        }

        var driver = GetOrCreateDriver(driverNumber);
        var timing = canonicalEvent.Payload["timing"];
        driver.Position = FirstNonNull(TryParseInt(timing?["Position"]?.ToString()), driver.Position);
        driver.Line = FirstNonNull(TryParseInt(timing?["Line"]?.ToString()), driver.Line);
        driver.GapToLeader = FirstNonEmpty(ExtractTimingValue(timing?["GapToLeader"]), driver.GapToLeader);
        driver.IntervalToPositionAhead = FirstNonEmpty(ExtractTimingValue(timing?["IntervalToPositionAhead"]), driver.IntervalToPositionAhead);
        driver.IsCatchingAhead = TryParseBool(timing?["IntervalToPositionAhead"]?["Catching"]?.ToString());
        driver.InPit = TryParseBool(timing?["InPit"]?.ToString()) ?? driver.InPit;
        driver.PitOut = TryParseBool(timing?["PitOut"]?.ToString()) ?? driver.PitOut;
        driver.Retired = TryParseBool(timing?["Retired"]?.ToString()) ?? driver.Retired;
        driver.Stopped = TryParseBool(timing?["Stopped"]?.ToString()) ?? driver.Stopped;
        driver.Status = FirstNonNull(TryParseInt(timing?["Status"]?.ToString()), driver.Status);
        driver.BestLapTime = FirstNonEmpty(ExtractTimingValue(timing?["BestLapTime"]), driver.BestLapTime);
        driver.LastLapTime = FirstNonEmpty(ExtractTimingValue(timing?["LastLapTime"]), driver.LastLapTime);

        if (timing?["Sectors"] is not null)
        {
            driver.Sectors = NormalizeSectors(timing["Sectors"]);
        }

        if (timing?["Speeds"] is JsonObject speeds)
        {
            driver.Speeds = speeds.DeepClone().AsObject();
        }

        return true;
    }

    private bool ApplyTimingStats(CanonicalEvent canonicalEvent)
    {
        if (canonicalEvent.DriverNumber is not int driverNumber)
        {
            return false;
        }

        var driver = GetOrCreateDriver(driverNumber);
        var timingStats = canonicalEvent.Payload["timingStats"];
        driver.Line = FirstNonNull(TryParseInt(timingStats?["Line"]?.ToString()), driver.Line);
        driver.BestLapTime = FirstNonEmpty(ExtractTimingValue(timingStats?["PersonalBestLapTime"]), driver.BestLapTime);
        if (timingStats?["BestSectors"] is not null)
        {
            driver.Sectors = NormalizeSectors(timingStats["BestSectors"]);
        }

        if (timingStats?["BestSpeeds"] is JsonObject bestSpeeds)
        {
            driver.Speeds = bestSpeeds.DeepClone().AsObject();
        }
        return true;
    }

    private bool ApplyTimingApp(CanonicalEvent canonicalEvent)
    {
        if (canonicalEvent.DriverNumber is not int driverNumber)
        {
            return false;
        }

        var driver = GetOrCreateDriver(driverNumber);
        var payload = canonicalEvent.Payload["timingApp"];
        driver.Line = FirstNonNull(TryParseInt(payload?["Line"]?.ToString()), driver.Line);
        driver.GridPosition = FirstNonNull(TryParseInt(payload?["GridPos"]?.ToString()), driver.GridPosition);

        if (payload?["Stints"] is JsonObject stints)
        {
            driver.TyreStints = stints.DeepClone().AsObject();
            driver.CurrentStintLapCount = ResolveCurrentStintLapCount(driver.TyreStints);
            var currentStint = ResolveCurrentStint(driver.TyreStints);
            driver.TyreCompound = FirstNonEmpty(currentStint?["Compound"]?.ToString(), driver.TyreCompound);
            driver.TyreIsNew = TryParseBool(currentStint?["New"]?.ToString()) ?? driver.TyreIsNew;
        }

        return true;
    }

    private bool ApplyCurrentTyres(CanonicalEvent canonicalEvent)
    {
        if (canonicalEvent.DriverNumber is not int driverNumber)
        {
            return false;
        }

        var driver = GetOrCreateDriver(driverNumber);
        var tyres = canonicalEvent.Payload["tyres"];
        driver.TyreCompound = FirstNonEmpty(tyres?["Compound"]?.ToString(), driver.TyreCompound);
        driver.TyreIsNew = TryParseBool(tyres?["New"]?.ToString()) ?? driver.TyreIsNew;
        return true;
    }

    private bool ApplyTyreStints(CanonicalEvent canonicalEvent)
    {
        if (canonicalEvent.DriverNumber is not int driverNumber)
        {
            return false;
        }

        var driver = GetOrCreateDriver(driverNumber);
        if (canonicalEvent.Payload["stints"] is not JsonObject stints)
        {
            return false;
        }

        driver.TyreStints = stints.DeepClone().AsObject();
        driver.CurrentStintLapCount = ResolveCurrentStintLapCount(driver.TyreStints);
        var currentStint = ResolveCurrentStint(driver.TyreStints);
        driver.TyreCompound = FirstNonEmpty(currentStint?["Compound"]?.ToString(), driver.TyreCompound);
        driver.TyreIsNew = TryParseBool(currentStint?["New"]?.ToString()) ?? driver.TyreIsNew;
        return true;
    }

    private bool ApplyPitLaneTime(CanonicalEvent canonicalEvent)
    {
        if (canonicalEvent.DriverNumber is not int driverNumber)
        {
            return false;
        }

        var driver = GetOrCreateDriver(driverNumber);
        var pit = canonicalEvent.Payload["pit"]?.DeepClone();
        if (pit is null)
        {
            return false;
        }

        driver.PitStops.Add(pit);
        while (driver.PitStops.Count > 10)
        {
            driver.PitStops.RemoveAt(0);
        }

        return true;
    }

    private bool ApplyPosition(CanonicalEvent canonicalEvent)
    {
        if (canonicalEvent.DriverNumber is not int driverNumber)
        {
            return false;
        }

        var driver = GetOrCreateDriver(driverNumber);
        if (canonicalEvent.Payload["position"] is JsonObject position)
        {
            driver.CurrentTrackPosition = position.DeepClone().AsObject();
            driver.CurrentTrackPositionTimestamp = TryParseDateTimeOffset(position["timestamp"]?.ToString()) ?? driver.CurrentTrackPositionTimestamp;
        }

        driver.LastPositionPacket = canonicalEvent.Payload.DeepClone().AsObject();
        return true;
    }

    private bool ApplyTelemetry(CanonicalEvent canonicalEvent)
    {
        if (canonicalEvent.DriverNumber is not int driverNumber)
        {
            return false;
        }

        var driver = GetOrCreateDriver(driverNumber);
        if (canonicalEvent.Payload["telemetry"] is JsonObject telemetry)
        {
            driver.Rpm = FirstNonNull(TryParseInt(telemetry["rpm"]?.ToString()), driver.Rpm);
            driver.Speed = FirstNonNull(TryParseInt(telemetry["speed"]?.ToString()), driver.Speed);
            driver.Gear = FirstNonNull(TryParseInt(telemetry["gear"]?.ToString()), driver.Gear);
            driver.Throttle = FirstNonNull(TryParseInt(telemetry["throttle"]?.ToString() ?? telemetry["throttlePct"]?.ToString()), driver.Throttle);
            driver.Brake = FirstNonNull(TryParseInt(telemetry["brake"]?.ToString()), driver.Brake);
            driver.Drs = FirstNonNull(TryParseInt(telemetry["drs"]?.ToString()), driver.Drs);
        }

        driver.LastTelemetryPacket = canonicalEvent.Payload.DeepClone().AsObject();
        return true;
    }

    private DriverRaceState GetOrCreateDriver(int driverNumber)
    {
        if (!_snapshot.Drivers.TryGetValue(driverNumber, out var driver))
        {
            driver = new DriverRaceState { DriverNumber = driverNumber };
            _snapshot.Drivers[driverNumber] = driver;
        }

        return driver;
    }

    private void StoreCanonicalEventState(CanonicalEvent canonicalEvent)
    {
        var eventState = new JsonObject
        {
            ["eventTime"] = canonicalEvent.EventTime,
            ["publishTime"] = canonicalEvent.PublishTime,
            ["sequence"] = canonicalEvent.Sequence,
            ["sourceFeed"] = canonicalEvent.Source.SourceFeed,
            ["deviceId"] = canonicalEvent.Source.DeviceId,
            ["payload"] = canonicalEvent.Payload.DeepClone()
        };

        if (canonicalEvent.DriverNumber is int driverNumber)
        {
            var driver = GetOrCreateDriver(driverNumber);
            driver.FeedState[canonicalEvent.EventType] = eventState;
            return;
        }

        _snapshot.GlobalFeedState[canonicalEvent.EventType] = eventState;
    }

    private static RaceStateSnapshot CloneSnapshot(RaceStateSnapshot snapshot)
    {
        return new RaceStateSnapshot
        {
            SessionId = snapshot.SessionId,
            UpdatedAt = snapshot.UpdatedAt,
            LastProcessedEventTime = snapshot.LastProcessedEventTime,
            LastProcessedSequence = snapshot.LastProcessedSequence,
            SessionInfo = snapshot.SessionInfo?.DeepClone().AsObject(),
            TrackStatusCode = snapshot.TrackStatusCode,
            TrackStatusMessage = snapshot.TrackStatusMessage,
            CurrentLap = snapshot.CurrentLap,
            TotalLaps = snapshot.TotalLaps,
            GlobalFeedState = snapshot.GlobalFeedState.DeepClone().AsObject(),
            Drivers = snapshot.Drivers.ToDictionary(
                entry => entry.Key,
                entry => new DriverRaceState
                {
                    DriverNumber = entry.Value.DriverNumber,
                    BroadcastName = entry.Value.BroadcastName,
                    FullName = entry.Value.FullName,
                    Tla = entry.Value.Tla,
                    TeamName = entry.Value.TeamName,
                    TeamColor = entry.Value.TeamColor,
                    Position = entry.Value.Position,
                    Line = entry.Value.Line,
                    GridPosition = entry.Value.GridPosition,
                    GapToLeader = entry.Value.GapToLeader,
                    IntervalToPositionAhead = entry.Value.IntervalToPositionAhead,
                    IsCatchingAhead = entry.Value.IsCatchingAhead,
                    InPit = entry.Value.InPit,
                    PitOut = entry.Value.PitOut,
                    Retired = entry.Value.Retired,
                    Stopped = entry.Value.Stopped,
                    Status = entry.Value.Status,
                    BestLapTime = entry.Value.BestLapTime,
                    LastLapTime = entry.Value.LastLapTime,
                    Sectors = entry.Value.Sectors?.DeepClone().AsObject(),
                    Speeds = entry.Value.Speeds?.DeepClone().AsObject(),
                    TyreCompound = entry.Value.TyreCompound,
                    TyreIsNew = entry.Value.TyreIsNew,
                    TyreStints = entry.Value.TyreStints?.DeepClone().AsObject(),
                    CurrentStintLapCount = entry.Value.CurrentStintLapCount,
                    PitStops = new JsonArray(entry.Value.PitStops.Select(message => message?.DeepClone()).ToArray()),
                    CurrentTrackPosition = entry.Value.CurrentTrackPosition?.DeepClone().AsObject(),
                    CurrentTrackPositionTimestamp = entry.Value.CurrentTrackPositionTimestamp,
                    LastPositionPacket = entry.Value.LastPositionPacket?.DeepClone().AsObject(),
                    Rpm = entry.Value.Rpm,
                    Speed = entry.Value.Speed,
                    Gear = entry.Value.Gear,
                    Throttle = entry.Value.Throttle,
                    Brake = entry.Value.Brake,
                    Drs = entry.Value.Drs,
                    LastTelemetryPacket = entry.Value.LastTelemetryPacket?.DeepClone().AsObject(),
                    FeedState = entry.Value.FeedState.DeepClone().AsObject()
                })
        };
    }

    private static int? TryParseInt(string? value) => int.TryParse(value, out var parsed) ? parsed : null;
    private static bool? TryParseBool(string? value) => bool.TryParse(value, out var parsed) ? parsed : null;
    private static DateTimeOffset? TryParseDateTimeOffset(string? value) => DateTimeOffset.TryParse(value, out var parsed) ? parsed : null;
    private static int? FirstNonNull(int? candidate, int? fallback) => candidate ?? fallback;
    private static string? FirstNonEmpty(string? candidate, string? fallback) => string.IsNullOrWhiteSpace(candidate) ? fallback : candidate;

    private static string? ExtractTimingValue(JsonNode? node)
    {
        if (node is null)
        {
            return null;
        }

        if (node is JsonObject jsonObject)
        {
            return jsonObject["Value"]?.ToString();
        }

        return node.ToString();
    }

    private static string? NormalizeColor(string? rawColor)
    {
        if (string.IsNullOrWhiteSpace(rawColor))
        {
            return null;
        }

        var color = rawColor.Trim();
        return color.StartsWith('#') ? color : $"#{color}";
    }

    private static string? BuildFullName(string? firstName, string? lastName)
    {
        var combined = $"{firstName} {lastName}".Trim();
        return string.IsNullOrWhiteSpace(combined) ? null : combined;
    }

    private static JsonObject? ResolveCurrentStint(JsonObject? stints)
    {
        if (stints is null)
        {
            return null;
        }

        var best = stints
            .Select(pair => (Index: TryParseInt(pair.Key), Node: pair.Value as JsonObject))
            .Where(item => item.Index is not null && item.Node is not null)
            .OrderByDescending(item => item.Index)
            .FirstOrDefault();

        return best.Node;
    }

    private static int? ResolveCurrentStintLapCount(JsonObject? stints)
        => FirstNonNull(TryParseInt(ResolveCurrentStint(stints)?["TotalLaps"]?.ToString()), null);

    private static JsonObject? NormalizeSectors(JsonNode? sectorsNode)
    {
        if (sectorsNode is JsonArray sectorArray)
        {
            var normalized = new JsonObject();
            for (var index = 0; index < sectorArray.Count; index++)
            {
                if (sectorArray[index] is not null)
                {
                    normalized[index.ToString()] = NormalizeSectorNode(sectorArray[index]!);
                }
            }

            return normalized;
        }

        if (sectorsNode is JsonObject sectorsObject)
        {
            var normalized = new JsonObject();
            foreach (var entry in sectorsObject)
            {
                if (entry.Value is not null)
                {
                    normalized[entry.Key] = NormalizeSectorNode(entry.Value);
                }
            }

            return normalized;
        }

        return null;
    }

    private static JsonNode NormalizeSectorNode(JsonNode sectorNode)
    {
        if (sectorNode is not JsonObject sectorObject)
        {
            return sectorNode.DeepClone();
        }

        var normalized = sectorObject.DeepClone().AsObject();
        if (normalized["Segments"] is JsonArray segmentsArray)
        {
            var normalizedSegments = new JsonObject();
            for (var index = 0; index < segmentsArray.Count; index++)
            {
                normalizedSegments[index.ToString()] = segmentsArray[index]?.DeepClone();
            }

            normalized["Segments"] = normalizedSegments;
        }

        return normalized;
    }

    private static JsonObject? CloneAsObject(JsonNode? node)
        => node is JsonObject jsonObject ? jsonObject.DeepClone().AsObject() : null;

    private static JsonObject? MergeJsonObject(JsonObject? current, JsonObject? incoming)
    {
        if (incoming is null)
        {
            return current;
        }

        if (current is null)
        {
            return incoming.DeepClone().AsObject();
        }

        var merged = current.DeepClone().AsObject();
        foreach (var entry in incoming)
        {
            merged[entry.Key] = MergeJsonNode(merged[entry.Key], entry.Value);
        }

        return merged;
    }

    private static JsonNode? MergeJsonNode(JsonNode? current, JsonNode? incoming)
    {
        if (incoming is null)
        {
            return current?.DeepClone();
        }

        if (current is JsonObject currentObject && incoming is JsonObject incomingObject)
        {
            return MergeJsonObject(currentObject, incomingObject);
        }

        return incoming.DeepClone();
    }

    private static string? ResolveStateKey(CanonicalEvent canonicalEvent)
        => canonicalEvent.EventType switch
        {
            "session.info.updated" => "session.info",
            "track.status.updated" => "track.status",
            "lap.count.updated" => "lap.count",
            "driver.list.updated" when canonicalEvent.DriverNumber is int driverNumber => $"driver.metadata:{driverNumber}",
            "timing.driver.updated" when canonicalEvent.DriverNumber is int driverNumber => $"driver.timing:{driverNumber}",
            "timing.stats.updated" when canonicalEvent.DriverNumber is int driverNumber => $"driver.timing-stats:{driverNumber}",
            "timing.app.updated" when canonicalEvent.DriverNumber is int driverNumber => $"driver.timing-app:{driverNumber}",
            "tyres.current.updated" when canonicalEvent.DriverNumber is int driverNumber => $"driver.tyres-current:{driverNumber}",
            "tyres.stint.updated" when canonicalEvent.DriverNumber is int driverNumber => $"driver.tyres-stint:{driverNumber}",
            "pitlane.time.updated" when canonicalEvent.DriverNumber is int driverNumber => $"driver.pitlane:{driverNumber}",
            "car.position.updated" when canonicalEvent.DriverNumber is int driverNumber => $"driver.position:{driverNumber}",
            "car.position.updated" => "driver.position:global",
            "car.telemetry.updated" when canonicalEvent.DriverNumber is int driverNumber => $"driver.telemetry:{driverNumber}",
            "car.telemetry.updated" => "driver.telemetry:global",
            _ => null
        };
}

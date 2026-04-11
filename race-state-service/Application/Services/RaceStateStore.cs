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
                "lap.series.updated" => ApplyLapSeries(canonicalEvent),
                "tyres.current.updated" => ApplyCurrentTyres(canonicalEvent),
                "tyres.stint.updated" => ApplyTyreStints(canonicalEvent),
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
        var explicitInPit = TryParseBool(timing?["InPit"]?.ToString());
        var explicitPitOut = TryParseBool(timing?["PitOut"]?.ToString());
        var explicitRetired = TryParseBool(timing?["Retired"]?.ToString());
        var explicitStopped = TryParseBool(timing?["Stopped"]?.ToString());
        var incomingStatus = TryParseInt(timing?["Status"]?.ToString());

        driver.Retired = explicitRetired ?? driver.Retired;
        driver.Stopped = explicitStopped ?? driver.Stopped;
        driver.Status = FirstNonNull(incomingStatus, driver.Status);
        driver.BestLapTime = FirstNonEmpty(ExtractTimingValue(timing?["BestLapTime"]), driver.BestLapTime);
        driver.LastLapTime = FirstNonEmpty(ExtractTimingValue(timing?["LastLapTime"]), driver.LastLapTime);
        driver.Sectors = MergeJsonObject(driver.Sectors, timing?["Sectors"]);
        driver.Speeds = MergeJsonObject(driver.Speeds, timing?["Speeds"]);

        ApplyDriverRaceStatus(driver, explicitInPit, explicitPitOut, incomingStatus, driver.LastLapTime);

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

    private bool ApplyLapSeries(CanonicalEvent canonicalEvent)
    {
        if (canonicalEvent.DriverNumber is not int driverNumber)
        {
            return false;
        }

        var driver = GetOrCreateDriver(driverNumber);
        var lapSeries = canonicalEvent.Payload["lapSeries"];
        var (lapNumber, position) = ResolveLapSeriesPosition(lapSeries?["LapPosition"]);

        driver.LapSeriesPosition = FirstNonNull(position, driver.LapSeriesPosition);
        driver.LapsCompleted = FirstNonNull(driver.LapsCompleted, lapNumber) ?? lapNumber;

        if (driver.Position is null && position is not null)
        {
            driver.Position = position;
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
                    LapSeriesPosition = entry.Value.LapSeriesPosition,
                    LapsCompleted = entry.Value.LapsCompleted,
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

    private static void ApplyDriverRaceStatus(
        DriverRaceState driver,
        bool? explicitInPit,
        bool? explicitPitOut,
        int? incomingStatus,
        string? lastLapTime)
    {
        var effectiveStatus = incomingStatus ?? driver.Status;
        var normalizedLastLapTime = NormalizeTimingInstruction(lastLapTime);

        var inferredInPit = explicitInPit
            ?? (normalizedLastLapTime == "PIT IN" ? (bool?)true : null)
            ?? (IsPitStatus(effectiveStatus) ? (bool?)true : null);

        var inferredPitOut = explicitPitOut
            ?? (normalizedLastLapTime == "PIT OUT" ? (bool?)true : null)
            ?? (IsPitOutStatus(effectiveStatus) ? (bool?)true : null);

        if (IsRetiredStatus(effectiveStatus))
        {
            driver.Retired = true;
            driver.Stopped = true;
        }

        if (driver.Retired || driver.Stopped)
        {
            driver.InPit = false;
            driver.PitOut = false;
            return;
        }

        if (inferredInPit == true)
        {
            driver.InPit = true;
            driver.PitOut = false;
            return;
        }

        if (inferredPitOut == true)
        {
            driver.InPit = false;
            driver.PitOut = true;
            return;
        }

        if (explicitInPit == false || IsRunningStatus(effectiveStatus))
        {
            driver.InPit = false;
        }

        if (explicitPitOut == false || IsRunningStatus(effectiveStatus) || explicitInPit == true)
        {
            driver.PitOut = false;
        }
    }

    private static bool IsRunningStatus(int? status) => status == 64;
    private static bool IsPitStatus(int? status) => status == 80;
    private static bool IsPitOutStatus(int? status) => status is 96 or 608;
    private static bool IsRetiredStatus(int? status) => status == 92;

    private static (int? LapNumber, int? Position) ResolveLapSeriesPosition(JsonNode? lapPositionNode)
    {
        if (lapPositionNode is JsonObject lapPositions)
        {
            var latest = lapPositions
                .Select(entry => (LapNumber: TryParseInt(entry.Key), Position: TryParseInt(entry.Value?.ToString())))
                .Where(entry => entry.LapNumber is not null && entry.Position is not null)
                .OrderByDescending(entry => entry.LapNumber)
                .FirstOrDefault();

            return latest;
        }

        if (lapPositionNode is JsonArray lapPositionArray)
        {
            var position = lapPositionArray
                .Select(node => TryParseInt(node?.ToString()))
                .LastOrDefault(value => value is not null);

            return (null, position);
        }

        return (null, null);
    }

    private static string? NormalizeTimingInstruction(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim().ToUpperInvariant();
    }

    private static JsonObject? MergeJsonObject(JsonObject? current, JsonNode? updateNode)
    {
        if (updateNode is not JsonObject update)
        {
            return current;
        }

        var result = current?.DeepClone().AsObject() ?? new JsonObject();
        foreach (var property in update)
        {
            if (property.Value is JsonObject childUpdate)
            {
                var childCurrent = result[property.Key] as JsonObject;
                result[property.Key] = MergeJsonObject(childCurrent, childUpdate);
                continue;
            }

            if (property.Value is not null)
            {
                result[property.Key] = property.Value.DeepClone();
            }
        }

        return result;
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
            "lap.series.updated" when canonicalEvent.DriverNumber is int driverNumber => $"driver.lap-series:{driverNumber}",
            "tyres.current.updated" when canonicalEvent.DriverNumber is int driverNumber => $"driver.tyres-current:{driverNumber}",
            "tyres.stint.updated" when canonicalEvent.DriverNumber is int driverNumber => $"driver.tyres-stint:{driverNumber}",
            _ => null
        };
}

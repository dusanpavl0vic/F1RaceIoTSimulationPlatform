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
                return new RaceStateApplyResult(CloneSnapshot(_snapshot), false, "Event type does not mutate the current race state.");
            }

            var candidateVersion = new AppliedEventVersion(canonicalEvent.EventTime, canonicalEvent.Sequence);
            if (_lastAppliedEventVersions.TryGetValue(stateKey, out var currentVersion)
                && candidateVersion.CompareTo(currentVersion) <= 0)
            {
                return new RaceStateApplyResult(CloneSnapshot(_snapshot), false, $"Stale event ignored for key '{stateKey}'.");
            }

            _snapshot.UpdatedAt = DateTimeOffset.UtcNow;
            _snapshot.LastProcessedEventTime = canonicalEvent.EventTime;
            _snapshot.LastProcessedSequence = canonicalEvent.Sequence;

            var applied = canonicalEvent.EventType switch
            {
                "session.info.updated" => ApplySessionInfo(canonicalEvent),
                "track.status.updated" => ApplyTrackStatus(canonicalEvent),
                "lap.count.updated" => ApplyLapCount(canonicalEvent),
                "weather.updated" => ApplyWeather(canonicalEvent),
                "race-control.message" => ApplyRaceControlMessage(canonicalEvent),
                "driver.list.updated" => ApplyDriverMetadata(canonicalEvent),
                "timing.driver.updated" => ApplyTiming(canonicalEvent),
                "timing.app.updated" => ApplyTimingApp(canonicalEvent),
                "tyres.current.updated" => ApplyCurrentTyres(canonicalEvent),
                "tyres.stint.updated" => ApplyTyreStints(canonicalEvent),
                "pitlane.time.updated" => ApplyPitLaneTime(canonicalEvent),
                "car.position.updated" => ApplyPosition(canonicalEvent),
                "car.telemetry.updated" => ApplyTelemetry(canonicalEvent),
                "team-radio.capture" => ApplyTeamRadio(canonicalEvent),
                _ => false
            };

            if (!applied)
            {
                return new RaceStateApplyResult(CloneSnapshot(_snapshot), false, "Event type is unsupported for race state mutation.");
            }

            _lastAppliedEventVersions[stateKey] = candidateVersion;
            return new RaceStateApplyResult(CloneSnapshot(_snapshot), true, null);
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
        _snapshot.CurrentLap = TryParseInt(payload?["CurrentLap"]?.ToString());
        _snapshot.TotalLaps = TryParseInt(payload?["TotalLaps"]?.ToString());
        return true;
    }

    private bool ApplyWeather(CanonicalEvent canonicalEvent)
    {
        _snapshot.Weather = canonicalEvent.Payload["data"]?.AsObject()?.DeepClone().AsObject();
        return true;
    }

    private bool ApplyRaceControlMessage(CanonicalEvent canonicalEvent)
    {
        var payload = canonicalEvent.Payload["data"];
        if (payload is null)
        {
            return false;
        }

        _snapshot.RaceControlMessages.Add(payload.DeepClone());
        while (_snapshot.RaceControlMessages.Count > 25)
        {
            _snapshot.RaceControlMessages.RemoveAt(0);
        }

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
        driver.BroadcastName = payload?["BroadcastName"]?.ToString();
        driver.FullName = payload?["FullName"]?.ToString() ?? payload?["Tla"]?.ToString();
        driver.Tla = payload?["Tla"]?.ToString();
        driver.TeamName = payload?["TeamName"]?.ToString();
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
        driver.GapToLeader = FirstNonEmpty(timing?["GapToLeader"]?.ToString(), driver.GapToLeader);
        driver.IntervalToPositionAhead = FirstNonEmpty(timing?["IntervalToPositionAhead"]?["Value"]?.ToString(), driver.IntervalToPositionAhead);
        driver.IsCatchingAhead = TryParseBool(timing?["IntervalToPositionAhead"]?["Catching"]?.ToString());
        driver.InPit = TryParseBool(timing?["InPit"]?.ToString()) ?? driver.InPit;
        driver.PitOut = TryParseBool(timing?["PitOut"]?.ToString()) ?? driver.PitOut;
        driver.Retired = TryParseBool(timing?["Retired"]?.ToString()) ?? driver.Retired;
        driver.Stopped = TryParseBool(timing?["Stopped"]?.ToString()) ?? driver.Stopped;
        driver.Status = FirstNonNull(TryParseInt(timing?["Status"]?.ToString()), driver.Status);
        driver.BestLapTime = FirstNonEmpty(timing?["BestLapTime"]?["Value"]?.ToString() ?? timing?["BestLapTime"]?.ToString(), driver.BestLapTime);
        driver.LastLapTime = FirstNonEmpty(timing?["LastLapTime"]?["Value"]?.ToString() ?? timing?["LastLapTime"]?.ToString(), driver.LastLapTime);

        if (timing?["Sectors"] is JsonObject sectors)
        {
            driver.Sectors = sectors.DeepClone().AsObject();
        }

        if (timing?["Speeds"] is JsonObject speeds)
        {
            driver.Speeds = speeds.DeepClone().AsObject();
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
            driver.CurrentStintLapCount = ResolveCurrentStintLapCount(stints);
            var currentStint = ResolveCurrentStint(stints);
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
        driver.CurrentStintLapCount = ResolveCurrentStintLapCount(stints);
        var currentStint = ResolveCurrentStint(stints);
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

    private bool ApplyTeamRadio(CanonicalEvent canonicalEvent)
    {
        if (canonicalEvent.DriverNumber is not int driverNumber)
        {
            return false;
        }

        var capture = canonicalEvent.Payload["radio"]?.DeepClone();
        if (capture is null)
        {
            return false;
        }

        var driver = GetOrCreateDriver(driverNumber);
        driver.TeamRadioCaptures.Add(capture);
        while (driver.TeamRadioCaptures.Count > 10)
        {
            driver.TeamRadioCaptures.RemoveAt(0);
        }

        _snapshot.TeamRadioCaptures.Add(capture);
        while (_snapshot.TeamRadioCaptures.Count > 50)
        {
            _snapshot.TeamRadioCaptures.RemoveAt(0);
        }

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
            Weather = snapshot.Weather?.DeepClone().AsObject(),
            RaceControlMessages = new JsonArray(snapshot.RaceControlMessages.Select(message => message?.DeepClone()).ToArray()),
            TeamRadioCaptures = new JsonArray(snapshot.TeamRadioCaptures.Select(message => message?.DeepClone()).ToArray()),
            Drivers = snapshot.Drivers.ToDictionary(
                entry => entry.Key,
                entry => new DriverRaceState
                {
                    DriverNumber = entry.Value.DriverNumber,
                    BroadcastName = entry.Value.BroadcastName,
                    FullName = entry.Value.FullName,
                    Tla = entry.Value.Tla,
                    TeamName = entry.Value.TeamName,
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
                    TeamRadioCaptures = new JsonArray(entry.Value.TeamRadioCaptures.Select(message => message?.DeepClone()).ToArray())
                })
        };
    }

    private static int? TryParseInt(string? value) => int.TryParse(value, out var parsed) ? parsed : null;
    private static bool? TryParseBool(string? value) => bool.TryParse(value, out var parsed) ? parsed : null;
    private static DateTimeOffset? TryParseDateTimeOffset(string? value) => DateTimeOffset.TryParse(value, out var parsed) ? parsed : null;
    private static int? FirstNonNull(int? candidate, int? fallback) => candidate ?? fallback;
    private static string? FirstNonEmpty(string? candidate, string? fallback) => string.IsNullOrWhiteSpace(candidate) ? fallback : candidate;

    private static JsonObject? ResolveCurrentStint(JsonObject stints)
    {
        var best = stints
            .Select(pair => (Index: TryParseInt(pair.Key), Node: pair.Value as JsonObject))
            .Where(item => item.Index is not null && item.Node is not null)
            .OrderByDescending(item => item.Index)
            .FirstOrDefault();

        return best.Node;
    }

    private static int? ResolveCurrentStintLapCount(JsonObject stints)
        => FirstNonNull(TryParseInt(ResolveCurrentStint(stints)?["TotalLaps"]?.ToString()), null);

    private static string? ResolveStateKey(CanonicalEvent canonicalEvent)
        => canonicalEvent.EventType switch
        {
            "session.info.updated" => "session.info",
            "track.status.updated" => "track.status",
            "lap.count.updated" => "lap.count",
            "weather.updated" => "weather",
            "race-control.message" => "race-control",
            "driver.list.updated" when canonicalEvent.DriverNumber is int driverNumber => $"driver.metadata:{driverNumber}",
            "timing.driver.updated" when canonicalEvent.DriverNumber is int driverNumber => $"driver.timing:{driverNumber}",
            "timing.app.updated" when canonicalEvent.DriverNumber is int driverNumber => $"driver.timing-app:{driverNumber}",
            "tyres.current.updated" when canonicalEvent.DriverNumber is int driverNumber => $"driver.tyres-current:{driverNumber}",
            "tyres.stint.updated" when canonicalEvent.DriverNumber is int driverNumber => $"driver.tyres-stint:{driverNumber}",
            "pitlane.time.updated" when canonicalEvent.DriverNumber is int driverNumber => $"driver.pitlane:{driverNumber}",
            "car.position.updated" when canonicalEvent.DriverNumber is int driverNumber => $"driver.position:{driverNumber}",
            "car.position.updated" => "driver.position:global",
            "car.telemetry.updated" when canonicalEvent.DriverNumber is int driverNumber => $"driver.telemetry:{driverNumber}",
            "car.telemetry.updated" => "driver.telemetry:global",
            "team-radio.capture" when canonicalEvent.DriverNumber is int driverNumber => $"driver.team-radio:{driverNumber}:{canonicalEvent.Sequence}",
            _ => null
        };
}

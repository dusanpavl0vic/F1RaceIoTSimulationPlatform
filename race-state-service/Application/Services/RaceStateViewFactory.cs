using System.Text.Json.Nodes;
using F1.RaceState.Service.Application.Models;
using F1.RaceState.Service.Domain.Models;

namespace F1.RaceState.Service.Application.Services;

public sealed class RaceStateViewFactory
{
    public IReadOnlyList<RaceLeaderboardEntryModel> BuildLeaderboard(RaceStateSnapshot snapshot)
        => snapshot.Drivers.Values
            .OrderBy(driver => IsInactiveDriver(driver) ? 1 : 0)
            .ThenBy(driver => driver.Position ?? int.MaxValue)
            .ThenBy(driver => driver.Line ?? int.MaxValue)
            .ThenBy(driver => driver.DriverNumber)
            .Select(BuildLeaderboardEntry)
            .ToArray();

    public IReadOnlyList<RaceMapPositionEntryModel> BuildMapPositions(RaceStateSnapshot snapshot)
        => snapshot.Drivers.Values
            .Where(driver => driver.CurrentTrackPosition is not null)
            .OrderBy(driver => driver.Position ?? int.MaxValue)
            .ThenBy(driver => driver.DriverNumber)
            .Select(driver => new RaceMapPositionEntryModel(
                driver.DriverNumber,
                ResolveDriverName(driver),
                driver.Position,
                driver.CurrentTrackPosition?["status"]?.ToString() ?? "-",
                TryParseInt(driver.CurrentTrackPosition?["x"]?.ToString()) ?? 0,
                TryParseInt(driver.CurrentTrackPosition?["y"]?.ToString()) ?? 0,
                TryParseInt(driver.CurrentTrackPosition?["z"]?.ToString()) ?? 0,
                driver.CurrentTrackPositionTimestamp,
                TryParseBool(driver.CurrentTrackPosition?["isEstimated"]?.ToString()) ?? false))
            .ToArray();

    public RaceDashboardViewModel BuildDashboard(RaceStateSnapshot snapshot)
        => new(
            BuildSessionView(snapshot),
            BuildLeaderboard(snapshot));

    public RaceCurrentStateViewModel BuildCurrentState(RaceStateSnapshot snapshot)
        => new(
            snapshot.SessionId,
            snapshot.UpdatedAt,
            snapshot.LastProcessedEventTime,
            snapshot.LastProcessedSequence,
            BuildCurrentSessionState(snapshot),
            snapshot.GlobalFeedState.DeepClone().AsObject(),
            BuildDriverFeedState(snapshot),
            BuildLeaderboard(snapshot));

    public RaceStateBroadcastMessage BuildBroadcastMessage(RaceStateSnapshot snapshot)
        => new(
            "race.state.updated",
            DateTimeOffset.UtcNow,
            BuildDashboard(snapshot));

    private static RaceSessionViewModel BuildSessionView(RaceStateSnapshot snapshot)
        => new(
            snapshot.SessionId,
            snapshot.SessionInfo?.DeepClone().AsObject(),
            snapshot.TrackStatusCode,
            snapshot.TrackStatusMessage,
            snapshot.CurrentLap,
            snapshot.TotalLaps,
            snapshot.LastProcessedEventTime,
            snapshot.LastProcessedSequence,
            snapshot.UpdatedAt);

    private static JsonObject BuildCurrentSessionState(RaceStateSnapshot snapshot)
        => new()
        {
            ["sessionId"] = snapshot.SessionId,
            ["session.info.updated"] = snapshot.SessionInfo?.DeepClone(),
            ["track.status.updated"] = new JsonObject
            {
                ["status"] = snapshot.TrackStatusCode,
                ["message"] = snapshot.TrackStatusMessage
            },
            ["lap.count.updated"] = new JsonObject
            {
                ["currentLap"] = snapshot.CurrentLap,
                ["totalLaps"] = snapshot.TotalLaps
            }
        };

    private static JsonObject BuildDriverFeedState(RaceStateSnapshot snapshot)
    {
        var drivers = new JsonObject();
        foreach (var driver in snapshot.Drivers.Values.OrderBy(driver => driver.DriverNumber))
        {
            drivers[driver.DriverNumber.ToString()] = BuildDriverState(driver);
        }

        return drivers;
    }

    private static JsonObject BuildDriverState(DriverRaceState driver)
        => new()
        {
            ["driverNumber"] = driver.DriverNumber,
            ["broadcastName"] = driver.BroadcastName,
            ["fullName"] = driver.FullName,
            ["tla"] = driver.Tla,
            ["team"] = new JsonObject
            {
                ["name"] = driver.TeamName,
                ["color"] = driver.TeamColor
            },
            ["leaderboard"] = new JsonObject
            {
                ["position"] = driver.Position,
                ["line"] = driver.Line,
                ["gridPosition"] = driver.GridPosition,
                ["gapToLeader"] = ResolveGapToLeader(driver),
                ["intervalToPositionAhead"] = driver.Position == 1 ? "-" : NormalizeTimingLabel(driver.IntervalToPositionAhead),
                ["bestLapTime"] = NormalizeTimingLabel(driver.BestLapTime),
                ["lastLapTime"] = NormalizeTimingLabel(driver.LastLapTime),
                ["sectors"] = driver.Sectors?.DeepClone(),
                ["speeds"] = driver.Speeds?.DeepClone()
            },
            ["tyres"] = new JsonObject
            {
                ["compound"] = driver.TyreCompound,
                ["isNew"] = driver.TyreIsNew,
                ["currentStintLapCount"] = driver.CurrentStintLapCount,
                ["stints"] = driver.TyreStints?.DeepClone()
            },
            ["race"] = new JsonObject
            {
                ["inPit"] = driver.InPit,
                ["pitOut"] = driver.PitOut,
                ["retired"] = driver.Retired,
                ["stopped"] = driver.Stopped,
                ["status"] = driver.Status,
                ["pitStops"] = driver.PitStops.DeepClone()
            },
            ["trackPosition"] = driver.CurrentTrackPosition?.DeepClone(),
            ["telemetry"] = new JsonObject
            {
                ["rpm"] = driver.Rpm,
                ["speed"] = driver.Speed,
                ["gear"] = driver.Gear,
                ["throttle"] = driver.Throttle,
                ["brake"] = driver.Brake,
                ["drs"] = driver.Drs,
                ["lastTelemetryPacket"] = driver.LastTelemetryPacket?.DeepClone()
            },
            ["feeds"] = driver.FeedState.DeepClone()
        };

    private static RaceLeaderboardEntryModel BuildLeaderboardEntry(DriverRaceState driver)
        => new(
            driver.DriverNumber,
            driver.BroadcastName,
            driver.FullName,
            driver.Tla,
            driver.TeamName,
            driver.TeamColor,
            driver.Position,
            driver.Line,
            driver.GridPosition,
            ResolveGapToLeader(driver),
            NormalizeTimingLabel(driver.IntervalToPositionAhead),
            driver.IsCatchingAhead,
            driver.InPit,
            driver.PitOut,
            driver.Retired,
            driver.Stopped,
            driver.Status,
            driver.BestLapTime,
            driver.LastLapTime,
            driver.Sectors?.DeepClone().AsObject(),
            driver.Speeds?.DeepClone().AsObject(),
            driver.TyreCompound,
            driver.TyreIsNew,
            driver.TyreStints?.DeepClone().AsObject(),
            driver.CurrentStintLapCount,
            new(driver.PitStops.Select(item => item?.DeepClone()).ToArray()),
            driver.CurrentTrackPosition?.DeepClone().AsObject(),
            driver.CurrentTrackPositionTimestamp,
            driver.LastPositionPacket?.DeepClone().AsObject(),
            driver.Rpm,
            driver.Speed,
            driver.Gear,
            driver.Throttle,
            driver.Brake,
            driver.Drs,
            driver.LastTelemetryPacket?.DeepClone().AsObject());

    private static string ResolveDriverName(DriverRaceState driver)
        => !string.IsNullOrWhiteSpace(driver.BroadcastName)
            ? driver.BroadcastName!
            : !string.IsNullOrWhiteSpace(driver.FullName)
                ? driver.FullName!
                : driver.DriverNumber.ToString();

    private static bool IsInactiveDriver(DriverRaceState driver)
        => driver.Retired || driver.Stopped;

    private static string ResolveGapToLeader(DriverRaceState driver)
        => driver.Position == 1
            ? "leader"
            : NormalizeTimingLabel(driver.GapToLeader);

    private static string NormalizeTimingLabel(string? value)
        => string.IsNullOrWhiteSpace(value)
            || value.TrimStart().StartsWith('{')
            || value.Trim().StartsWith("LAP ", StringComparison.OrdinalIgnoreCase)
            ? "-"
            : value;

    private static int? TryParseInt(string? value) => int.TryParse(value, out var parsed) ? parsed : null;
    private static bool? TryParseBool(string? value) => bool.TryParse(value, out var parsed) ? parsed : null;
}

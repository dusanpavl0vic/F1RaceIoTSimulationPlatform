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
            .ThenBy(driver => driver.GridPosition ?? int.MaxValue)
            .ThenBy(driver => driver.Line ?? int.MaxValue)
            .ThenBy(driver => driver.DriverNumber)
            .Select(BuildLeaderboardEntry)
            .ToArray();

    public IReadOnlyList<RaceMapPositionEntryModel> BuildMapPositions(RaceStateSnapshot snapshot)
        => snapshot.Drivers.Values
            .Where(driver => driver.CurrentTrackPosition is not null)
            .OrderBy(driver => IsInactiveDriver(driver) ? 1 : 0)
            .ThenBy(driver => driver.Position ?? int.MaxValue)
            .ThenBy(driver => driver.GridPosition ?? int.MaxValue)
            .ThenBy(driver => driver.Line ?? int.MaxValue)
            .ThenBy(driver => driver.DriverNumber)
            .Select(driver => new RaceMapPositionEntryModel(
                driver.DriverNumber,
                ResolveDriverName(driver),
                ResolveLeaderboardOrder(driver),
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
            BuildDriverFeedState(snapshot),
            BuildLeaderboard(snapshot));

    public RaceStateBroadcastMessage BuildBroadcastMessage(RaceStateSnapshot snapshot)
        => new(
            "race.state.updated",
            DateTimeOffset.UtcNow,
            BuildDashboard(snapshot),
            BuildCurrentState(snapshot));

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
                ["leaderboardOrder"] = ResolveLeaderboardOrder(driver),
                ["gridPosition"] = driver.GridPosition,
                ["gapToLeader"] = ResolveGapToLeader(driver),
                ["intervalToPositionAhead"] = IsLeaderboardLeader(driver) ? "-" : NormalizeTimingLabel(driver.IntervalToPositionAhead),
                ["bestLapTime"] = NormalizeTimingLabel(driver.BestLapTime),
                ["lastLapTime"] = NormalizeTimingLabel(driver.LastLapTime)
            },
            ["tyres"] = new JsonObject
            {
                ["compound"] = driver.TyreCompound,
                ["isNew"] = driver.TyreIsNew,
                ["currentStintLapCount"] = driver.CurrentStintLapCount
            },
            ["race"] = new JsonObject
            {
                ["inPit"] = driver.InPit,
                ["pitOut"] = driver.PitOut,
                ["retired"] = driver.Retired,
                ["stopped"] = driver.Stopped,
                ["status"] = driver.Status
            }
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
            driver.TyreCompound,
            driver.TyreIsNew,
            driver.CurrentStintLapCount);

    private static string ResolveDriverName(DriverRaceState driver)
        => !string.IsNullOrWhiteSpace(driver.BroadcastName)
            ? driver.BroadcastName!
            : !string.IsNullOrWhiteSpace(driver.FullName)
                ? driver.FullName!
                : driver.DriverNumber.ToString();

    private static bool IsInactiveDriver(DriverRaceState driver)
        => driver.Retired || driver.Stopped;

    private static int? ResolveLeaderboardOrder(DriverRaceState driver)
        => driver.Position ?? driver.GridPosition ?? driver.Line;

    private static bool IsLeaderboardLeader(DriverRaceState driver)
        => driver.Position == 1
            || (driver.Position is null && driver.GridPosition == 1 && string.IsNullOrWhiteSpace(driver.GapToLeader));

    private static string ResolveGapToLeader(DriverRaceState driver)
        => IsLeaderboardLeader(driver)
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

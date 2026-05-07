using System.Text.Json.Nodes;
using F1.RaceState.Service.Application.Models;
using F1.RaceState.Service.Domain.Models;

namespace F1.RaceState.Service.Application.Services;

public sealed class RaceStateViewFactory
{
    public IReadOnlyList<RaceLeaderboardEntryModel> BuildLeaderboard(RaceStateSnapshot snapshot)
        => ComputeLeaderboard(snapshot);

    private static IReadOnlyList<RaceLeaderboardEntryModel> ComputeLeaderboard(RaceStateSnapshot snapshot)
        => snapshot.Drivers.Values
            .OrderBy(driver => ResolveDriverActivityBucket(snapshot, driver))
            .ThenBy(driver => ResolvePrimaryLeaderboardSignal(driver) ?? int.MaxValue)
            .ThenBy(driver => driver.Position ?? int.MaxValue)
            .ThenBy(driver => driver.LapSeriesPosition ?? int.MaxValue)
            .ThenBy(driver => driver.GridPosition ?? int.MaxValue)
            .ThenBy(driver => driver.Line ?? int.MaxValue)
            .ThenBy(driver => driver.DriverNumber)
            .Select((driver, index) => BuildLeaderboardEntry(snapshot, driver, index + 1))
            .ToArray();

    public IReadOnlyList<RaceMapPositionEntryModel> BuildMapPositions(RaceStateSnapshot snapshot)
        => snapshot.Drivers.Values
            .Where(driver => driver.CurrentTrackPosition is not null)
            .OrderBy(driver => ResolveDriverActivityBucket(snapshot, driver))
            .ThenBy(driver => ResolvePrimaryLeaderboardSignal(driver) ?? int.MaxValue)
            .ThenBy(driver => driver.GridPosition ?? int.MaxValue)
            .ThenBy(driver => driver.Line ?? int.MaxValue)
            .ThenBy(driver => driver.DriverNumber)
            .Select(driver => new RaceMapPositionEntryModel(
                driver.DriverNumber,
                ResolveDriverName(driver),
                ResolvePrimaryLeaderboardSignal(driver),
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
            BuildDriverFeedState(snapshot));

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
            ["lap.count.updated"] = new JsonObject
            {
                ["currentLap"] = snapshot.CurrentLap,
                ["totalLaps"] = snapshot.TotalLaps
            }
        };

    private static JsonObject BuildDriverFeedState(RaceStateSnapshot snapshot)
    {
        var drivers = new JsonObject();
        var displayPositions = ComputeLeaderboard(snapshot)
            .ToDictionary(entry => entry.DriverNumber, entry => entry.Position);

        foreach (var driver in snapshot.Drivers.Values.OrderBy(driver => driver.DriverNumber))
        {
            drivers[driver.DriverNumber.ToString()] = BuildDriverState(
                snapshot,
                driver,
                displayPositions.GetValueOrDefault(driver.DriverNumber));
        }

        return drivers;
    }

    private static JsonObject BuildDriverState(RaceStateSnapshot snapshot, DriverRaceState driver, int? displayPosition)
    {
        return new JsonObject
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
                ["position"] = displayPosition,
                ["displayPosition"] = displayPosition,
                ["timingPosition"] = driver.Position,
                ["line"] = driver.Line,
                ["leaderboardOrder"] = ResolvePrimaryLeaderboardSignal(driver),
                ["gridPosition"] = driver.GridPosition,
                ["lapSeriesPosition"] = driver.LapSeriesPosition,
                ["lapsCompleted"] = driver.LapsCompleted,
                ["gapToLeader"] = NormalizeTimingLabel(driver.GapToLeader),
                ["intervalToPositionAhead"] = NormalizeTimingLabel(driver.IntervalToPositionAhead),
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
                ["didNotStart"] = IsDidNotStart(snapshot, driver),
                ["status"] = driver.Status
            }
        };
    }

    private static RaceLeaderboardEntryModel BuildLeaderboardEntry(RaceStateSnapshot snapshot, DriverRaceState driver, int displayPosition)
    {
        var didNotStart = IsDidNotStart(snapshot, driver);

        return new RaceLeaderboardEntryModel(
            driver.DriverNumber,
            driver.BroadcastName,
            driver.FullName,
            driver.Tla,
            driver.TeamName,
            driver.TeamColor,
            displayPosition,
            driver.Line,
            driver.GridPosition,
            ResolveGapToLeader(driver, displayPosition),
            displayPosition == 1 ? "-" : NormalizeTimingLabel(driver.IntervalToPositionAhead),
            driver.IsCatchingAhead,
            driver.InPit,
            driver.PitOut,
            driver.Retired,
            driver.Stopped,
            didNotStart,
            driver.Status,
            NormalizeTimingLabel(driver.BestLapTime),
            NormalizeTimingLabel(driver.LastLapTime),
            driver.TyreCompound,
            driver.TyreIsNew,
            driver.CurrentStintLapCount);
    }

    private static string ResolveDriverName(DriverRaceState driver)
        => !string.IsNullOrWhiteSpace(driver.BroadcastName)
            ? driver.BroadcastName!
            : !string.IsNullOrWhiteSpace(driver.FullName)
                ? driver.FullName!
                : driver.DriverNumber.ToString();

    private static int ResolveDriverActivityBucket(RaceStateSnapshot snapshot, DriverRaceState driver)
        => IsDidNotStart(snapshot, driver)
            ? 2
            : driver.Retired || driver.Stopped
                ? 1
                : 0;

    private static int? ResolvePrimaryLeaderboardSignal(DriverRaceState driver)
        => driver.Position ?? driver.LapSeriesPosition ?? driver.GridPosition ?? driver.Line;

    private static bool IsDidNotStart(RaceStateSnapshot snapshot, DriverRaceState driver)
        => (snapshot.CurrentLap ?? 0) > 1
            && !driver.Retired
            && !driver.Stopped
            && driver.LapsCompleted is null
            && driver.LapSeriesPosition is null
            && driver.GridPosition is not null
            && driver.InPit
            && !driver.PitOut
            && driver.Status is 80 or 28;

    private static string ResolveGapToLeader(DriverRaceState driver, int displayPosition)
        => displayPosition == 1
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

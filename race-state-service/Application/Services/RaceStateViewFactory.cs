using F1.RaceState.Service.Application.Models;
using F1.RaceState.Service.Domain.Models;

namespace F1.RaceState.Service.Application.Services;

public sealed class RaceStateViewFactory
{
    public IReadOnlyList<RaceLeaderboardEntryModel> BuildLeaderboard(RaceStateSnapshot snapshot)
        => snapshot.Drivers.Values
            .OrderBy(driver => driver.GridPosition ?? int.MaxValue)
            .ThenBy(driver => driver.Line ?? int.MaxValue)
            .ThenBy(driver => driver.DriverNumber)
            .Select(BuildLeaderboardEntry)
            .ToArray();

    public IReadOnlyList<RaceMapPositionEntryModel> BuildMapPositions(RaceStateSnapshot snapshot)
        => snapshot.Drivers.Values
            .Where(driver => driver.CurrentTrackPosition is not null)
            .OrderBy(driver => driver.GridPosition ?? int.MaxValue)
            .ThenBy(driver => driver.Line ?? int.MaxValue)
            .ThenBy(driver => driver.DriverNumber)
            .Select(driver => new RaceMapPositionEntryModel(
                driver.DriverNumber,
                ResolveDriverName(driver),
                driver.GridPosition ?? driver.Line,
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
            BuildSessionView(snapshot),
            BuildLeaderboard(snapshot));

    public RaceStateSnapshotMessage BuildSnapshotMessage(RaceStateSnapshot snapshot)
        => new(
            "race.state.snapshot",
            DateTimeOffset.UtcNow,
            BuildDashboard(snapshot));

    public RaceStateChangeMessage BuildChangeMessage(
        RaceStateSnapshot snapshot,
        string? stateKey,
        string eventType,
        int? driverNumber,
        DateTimeOffset eventTime,
        long sequence)
    {
        var dashboard = BuildDashboard(snapshot);

        return new(
            "race.state.change",
            DateTimeOffset.UtcNow,
            stateKey,
            eventType,
            snapshot.SessionId ?? string.Empty,
            driverNumber,
            eventTime,
            sequence,
            dashboard,
            dashboard.Session,
            new
            {
                Section = "state",
                DriverNumber = driverNumber,
                EventType = eventType
            });
    }

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
            driver.GapToLeader,
            driver.IntervalToPositionAhead,
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

    private static int? TryParseInt(string? value) => int.TryParse(value, out var parsed) ? parsed : null;
    private static bool? TryParseBool(string? value) => bool.TryParse(value, out var parsed) ? parsed : null;
}

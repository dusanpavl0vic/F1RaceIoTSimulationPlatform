using F1.RaceState.Service.Application.Contracts;
using F1.RaceState.Service.Application.Models;

namespace F1.RaceState.Service.Application.Services;

public sealed class RaceStateReadService(IRaceStateStore raceStateStore, RaceStateViewFactory viewFactory)
{
    private readonly IRaceStateStore _raceStateStore = raceStateStore;
    private readonly RaceStateViewFactory _viewFactory = viewFactory;

    public RaceCurrentStateViewModel GetCurrent()
    {
        var snapshot = _raceStateStore.GetSnapshot();
        return _viewFactory.BuildCurrentState(snapshot);
    }

    public RaceRecoveryStateViewModel GetRecoveryState()
    {
        var checkpoint = _raceStateStore.GetCheckpoint();
        return new RaceRecoveryStateViewModel(
            checkpoint.Snapshot.SessionId,
            checkpoint.Snapshot.UpdatedAt,
            checkpoint.Snapshot.LastProcessedEventTime,
            checkpoint.Snapshot.LastProcessedSequence,
            checkpoint.Snapshot.Drivers.Count,
            checkpoint.LastAppliedEventVersions.Count);
    }

    public IReadOnlyList<RaceLeaderboardEntryModel> GetDrivers()
    {
        var snapshot = _raceStateStore.GetSnapshot();
        return _viewFactory.BuildLeaderboard(snapshot);
    }

    public RaceLeaderboardViewModel GetLeaderboard()
    {
        var snapshot = _raceStateStore.GetSnapshot();
        return new RaceLeaderboardViewModel(
            snapshot.SessionId,
            snapshot.CurrentLap,
            snapshot.TotalLaps,
            snapshot.TrackStatusCode,
            snapshot.TrackStatusMessage,
            _viewFactory.BuildLeaderboard(snapshot));
    }

    public RaceMapViewModel GetMap()
    {
        var snapshot = _raceStateStore.GetSnapshot();
        return new RaceMapViewModel(
            snapshot.SessionId,
            _viewFactory.BuildMapPositions(snapshot));
    }

    public RaceDashboardViewModel GetDashboard()
    {
        var snapshot = _raceStateStore.GetSnapshot();
        return _viewFactory.BuildDashboard(snapshot);
    }

}

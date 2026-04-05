namespace F1.RaceState.Service.Application.Models;

public sealed record RaceDashboardViewModel(
    object Session,
    IReadOnlyList<RaceLeaderboardEntryModel> Leaderboard,
    IReadOnlyList<RaceMapPositionEntryModel> MapPositions,
    object Snapshot);

namespace F1.RaceState.Service.Application.Models;

public sealed record RaceCurrentStateViewModel(
    RaceSessionViewModel Session,
    IReadOnlyList<RaceLeaderboardEntryModel> Leaderboard);

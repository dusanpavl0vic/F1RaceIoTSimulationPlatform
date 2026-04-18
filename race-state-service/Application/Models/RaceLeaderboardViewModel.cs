namespace F1.RaceState.Service.Application.Models;

public sealed record RaceLeaderboardViewModel(
    string? SessionId,
    int? CurrentLap,
    int? TotalLaps,
    string? TrackStatusCode,
    string? TrackStatusMessage,
    IReadOnlyList<RaceLeaderboardEntryModel> Drivers);

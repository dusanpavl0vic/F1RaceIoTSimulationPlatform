namespace F1.RaceState.Service.Application.Models;

public sealed record RaceMapViewModel(
    string? SessionId,
    IReadOnlyList<RaceMapPositionEntryModel> Positions);

namespace F1.RaceState.Service.Application.Models;

public sealed record RaceTelemetryMetadataViewModel(
    RaceSessionViewModel Session,
    IReadOnlyList<RaceTelemetryDriverSummaryViewModel> Drivers);

public sealed record RaceTelemetryDriverSummaryViewModel(
    int DriverNumber,
    string? BroadcastName,
    string? FullName,
    string? Tla,
    string? TeamName,
    string? TeamColor,
    int? Position,
    int? GridPosition);

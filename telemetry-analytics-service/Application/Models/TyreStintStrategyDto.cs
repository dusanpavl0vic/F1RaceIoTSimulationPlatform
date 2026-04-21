namespace F1.TelemetryAnalytics.Service.Application.Models;

public sealed record TyreStintStrategyDto(
    string SessionId,
    int TotalLaps,
    IReadOnlyList<TyreStintDriverDto> Drivers);

public sealed record TyreStintDriverDto(
    int DriverNumber,
    string DriverName,
    string? TeamName,
    string? TeamColor,
    int? GridPosition,
    int? Position,
    IReadOnlyList<TyreStintDto> Stints);

public sealed record TyreStintDto(
    int StintNumber,
    string? Compound,
    bool? TyreIsNew,
    int? StartLap,
    int? EndLap,
    int? LapCount);

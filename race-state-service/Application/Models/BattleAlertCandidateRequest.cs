namespace F1.RaceState.Service.Application.Models;

public sealed record BattleAlertCandidateRequest(
    string? SessionId,
    int DriverNumber,
    int? Position,
    double? GapSeconds,
    string? GapLabel,
    DateTimeOffset EventTime);

namespace F1.RaceState.Service.Application.Models;

public sealed record BattleAlertMessage(
    string Type,
    DateTimeOffset SentAt,
    string SessionId,
    int DriverNumber,
    string DriverLabel,
    int AheadDriverNumber,
    string AheadDriverLabel,
    int BattleForPosition,
    double? GapSeconds,
    string GapLabel,
    string Message);

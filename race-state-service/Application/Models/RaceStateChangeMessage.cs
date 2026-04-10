namespace F1.RaceState.Service.Application.Models;

public sealed record RaceStateChangeMessage(
    string Type,
    DateTimeOffset SentAt,
    string? StateKey,
    string EventType,
    string SessionId,
    int? DriverNumber,
    DateTimeOffset EventTime,
    long Sequence,
    RaceDashboardViewModel Dashboard,
    object Session,
    object Change);

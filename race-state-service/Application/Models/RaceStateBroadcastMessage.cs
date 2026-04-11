namespace F1.RaceState.Service.Application.Models;

public sealed record RaceStateBroadcastMessage(
    string Type,
    DateTimeOffset SentAt,
    RaceDashboardViewModel Dashboard,
    RaceCurrentStateViewModel CurrentState);

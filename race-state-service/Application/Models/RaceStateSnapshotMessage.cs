namespace F1.RaceState.Service.Application.Models;

public sealed record RaceStateSnapshotMessage(
    string Type,
    DateTimeOffset SentAt,
    RaceDashboardViewModel Dashboard);

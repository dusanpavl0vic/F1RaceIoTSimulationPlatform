namespace F1.RaceState.Service.Application.Queries;

public sealed record GetDriverLapTelemetryQuery(
    string SessionId,
    int DriverNumber,
    int LapNumber,
    IReadOnlyCollection<string>? Metrics);

namespace F1.RaceState.Service.Application.Queries;

public sealed record GetLatestDriverTelemetryQuery(
    string SessionId,
    int DriverNumber,
    int MaxSamples,
    DateTimeOffset? SinceTimestamp,
    IReadOnlyCollection<string>? Metrics);

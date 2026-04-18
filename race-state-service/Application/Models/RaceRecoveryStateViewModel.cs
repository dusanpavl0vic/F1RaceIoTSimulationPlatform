namespace F1.RaceState.Service.Application.Models;

public sealed record RaceRecoveryStateViewModel(
    string? SessionId,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? LastProcessedEventTime,
    long? LastProcessedSequence,
    int DriverCount,
    int VersionKeyCount);

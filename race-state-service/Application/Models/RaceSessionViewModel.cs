using System.Text.Json.Nodes;

namespace F1.RaceState.Service.Application.Models;

public sealed record RaceSessionViewModel(
    string? SessionId,
    JsonObject? SessionInfo,
    string? TrackStatusCode,
    string? TrackStatusMessage,
    int? CurrentLap,
    int? TotalLaps,
    DateTimeOffset? LastProcessedEventTime,
    long? LastProcessedSequence,
    DateTimeOffset UpdatedAt);

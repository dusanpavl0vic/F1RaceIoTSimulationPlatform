using F1.FeedReplay.Service.Domain.Models;

namespace F1.FeedReplay.Service.Application.Models;

public sealed record ReplayStatusModel(
    string? SessionId,
    ReplayRunState State,
    double ReplaySpeed,
    long PublishedEvents,
    long TotalEvents,
    DateTimeOffset? SimulationStartReference,
    TimeSpan CurrentVirtualTime,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    string? LastError);

namespace F1.FeedReplay.Service.Domain.Models;

public sealed record ReplaySession(
    string SessionId,
    ReplayConfiguration Configuration,
    DateTimeOffset SimulationStartReference,
    IReadOnlyList<ReplayEvent> Events);

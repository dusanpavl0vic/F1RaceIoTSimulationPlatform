namespace F1.FeedReplay.Service.Domain.Models;

public sealed record ReplayConfiguration(
    string SessionId,
    double ReplaySpeed,
    int StartOffsetMs,
    IReadOnlyList<FeedDefinition> Feeds);

namespace F1.FeedReplay.Service.API.Contracts;

public sealed record ReplayBootstrapRequest(
    string? SessionId,
    string[]? FeedNames);

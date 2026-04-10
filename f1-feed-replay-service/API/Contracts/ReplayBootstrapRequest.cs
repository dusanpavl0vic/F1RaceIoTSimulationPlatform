namespace F1.FeedReplay.Service.API.Contracts;

public sealed record ReplayBootstrapRequest(
    string? IndexUrl,
    string? ConfigurationPath,
    string? SessionId,
    string[]? FeedNames);

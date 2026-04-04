namespace F1.FeedReplay.Service.Application.Models;

public sealed record ReplayBootstrapParameters(
    string? IndexUrl,
    string? ConfigurationPath,
    string? SessionId,
    IReadOnlyCollection<string>? FeedNames,
    bool DownloadFeeds = true,
    bool ForceDownload = true,
    bool LoadAfterDownload = true,
    bool StartAfterLoad = false);

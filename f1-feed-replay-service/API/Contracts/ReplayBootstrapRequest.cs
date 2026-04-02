namespace F1.FeedReplay.Service.API.Contracts;

public sealed record ReplayBootstrapRequest(
    string? ConfigurationPath,
    string? IndexUrl,
    bool DownloadFeeds = true,
    bool ForceDownload = true,
    bool LoadAfterDownload = true,
    bool StartAfterLoad = false);

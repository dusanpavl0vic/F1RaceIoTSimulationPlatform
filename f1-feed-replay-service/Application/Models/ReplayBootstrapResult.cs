namespace F1.FeedReplay.Service.Application.Models;

public sealed record ReplayBootstrapResult(
    string SessionId,
    string StoragePath,
    string ConfigurationPath,
    string IndexUrl,
    int ConfiguredFeedCount,
    int DownloadedFeedCount,
    int SkippedFeedCount,
    IReadOnlyList<string> DownloadedFeeds,
    IReadOnlyList<string> SkippedFeeds,
    bool LoadedReplay,
    bool StartedReplay);

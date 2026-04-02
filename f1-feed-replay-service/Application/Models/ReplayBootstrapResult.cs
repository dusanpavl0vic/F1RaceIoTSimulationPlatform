namespace F1.FeedReplay.Service.Application.Models;

public sealed record ReplayBootstrapResult(
    string ConfigurationPath,
    string IndexUrl,
    int DownloadedFeedCount,
    int SkippedFeedCount,
    IReadOnlyList<string> DownloadedFeeds,
    IReadOnlyList<string> SkippedFeeds,
    bool LoadedReplay,
    bool StartedReplay);

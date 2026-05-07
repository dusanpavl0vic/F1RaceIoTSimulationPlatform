namespace F1.FeedReplay.Service.Application.Commands;

public sealed record BootstrapReplayCommand(
    string? SessionId,
    IReadOnlyCollection<string>? FeedNames,
    bool DownloadFeeds = true,
    bool ForceDownload = true,
    bool LoadAfterDownload = true,
    bool StartAfterLoad = false);

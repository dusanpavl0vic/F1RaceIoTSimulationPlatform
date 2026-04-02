using F1.FeedReplay.Service.Application.Models;

namespace F1.FeedReplay.Service.Application.Contracts;

public interface IReplayBootstrapper
{
    Task<ReplayBootstrapResult> BootstrapAsync(
        string? configurationPath,
        string? indexUrl,
        bool downloadFeeds,
        bool forceDownload,
        bool loadAfterDownload,
        bool startAfterLoad,
        CancellationToken cancellationToken);
}

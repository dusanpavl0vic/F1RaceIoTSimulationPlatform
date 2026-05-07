using F1.FeedReplay.Service.API.Contracts;
using F1.FeedReplay.Service.Application.Commands;

namespace F1.FeedReplay.Service.API.Mappers;

public static class ReplayRequestMapper
{
    public static BootstrapReplayCommand Map(ReplayBootstrapRequest request)
        => new(
            request.SessionId,
            request.FeedNames,
            DownloadFeeds: true,
            ForceDownload: false,
            LoadAfterDownload: true,
            StartAfterLoad: false);

    public static ChangeReplaySpeedCommand Map(ChangeReplaySpeedRequest request)
        => new(request.Speed);
}

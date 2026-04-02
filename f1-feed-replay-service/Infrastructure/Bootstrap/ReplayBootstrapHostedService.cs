using F1.FeedReplay.Service.Application.Contracts;

namespace F1.FeedReplay.Service.Infrastructure.Bootstrap;

public sealed class ReplayBootstrapHostedService(
    IConfiguration configuration,
    IReplayBootstrapper replayBootstrapper,
    ILogger<ReplayBootstrapHostedService> logger) : IHostedService
{
    private readonly IConfiguration _configuration = configuration;
    private readonly IReplayBootstrapper _replayBootstrapper = replayBootstrapper;
    private readonly ILogger<ReplayBootstrapHostedService> _logger = logger;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_configuration.GetValue<bool>("ReplayBootstrap:Enabled"))
        {
            return;
        }

        var indexUrl = _configuration["ReplayBootstrap:IndexUrl"];
        var downloadFeeds = _configuration.GetValue("ReplayBootstrap:DownloadOnStartup", false);
        var forceDownload = _configuration.GetValue("ReplayBootstrap:OverwriteExistingFiles", false);
        var loadAfterDownload = _configuration.GetValue("ReplayBootstrap:AutoLoadOnStartup", false);
        var startAfterLoad = _configuration.GetValue("ReplayBootstrap:AutoStartOnStartup", false);

        var result = await _replayBootstrapper.BootstrapAsync(
            string.Empty,
            indexUrl,
            downloadFeeds,
            forceDownload,
            loadAfterDownload,
            startAfterLoad,
            cancellationToken);

        _logger.LogInformation(
            "Replay bootstrap completed. Downloaded {DownloadedFeeds} feeds, skipped {SkippedFeeds}.",
            result.DownloadedFeedCount,
            result.SkippedFeedCount);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

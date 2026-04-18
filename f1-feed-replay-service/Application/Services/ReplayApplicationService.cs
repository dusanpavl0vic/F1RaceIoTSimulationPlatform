using F1.FeedReplay.Service.Application.Commands;
using F1.FeedReplay.Service.Application.Contracts;
using F1.FeedReplay.Service.Application.Models;

namespace F1.FeedReplay.Service.Application.Services;

public sealed class ReplayApplicationService(
    IReplayCoordinator replayCoordinator,
    IReplayBootstrapper replayBootstrapper)
{
    private readonly IReplayCoordinator _replayCoordinator = replayCoordinator;
    private readonly IReplayBootstrapper _replayBootstrapper = replayBootstrapper;

    public Task<ReplayBootstrapResult> BootstrapAsync(BootstrapReplayCommand command, CancellationToken cancellationToken)
        => _replayBootstrapper.BootstrapAsync(command, cancellationToken);

    public Task<ReplayStatusModel> LoadAsync(LoadReplayCommand command, CancellationToken cancellationToken)
        => _replayCoordinator.LoadAsync(command, cancellationToken);

    public Task<ReplayStatusModel> StartAsync(CancellationToken cancellationToken)
        => _replayCoordinator.StartAsync(new StartReplayCommand(), cancellationToken);

    public Task<ReplayStatusModel> PauseAsync(CancellationToken cancellationToken)
        => _replayCoordinator.PauseAsync(new PauseReplayCommand(), cancellationToken);

    public Task<ReplayStatusModel> ResumeAsync(CancellationToken cancellationToken)
        => _replayCoordinator.ResumeAsync(new ResumeReplayCommand(), cancellationToken);

    public Task<ReplayStatusModel> StopAsync(CancellationToken cancellationToken)
        => _replayCoordinator.StopAsync(new StopReplayCommand(), cancellationToken);

    public Task<ReplayStatusModel> ChangeReplaySpeedAsync(ChangeReplaySpeedCommand command, CancellationToken cancellationToken)
        => _replayCoordinator.ChangeReplaySpeedAsync(command, cancellationToken);

    public ReplayStatusModel GetStatus()
        => _replayCoordinator.GetStatus();
}

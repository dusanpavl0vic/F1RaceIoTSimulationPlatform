using F1.FeedReplay.Service.Application.Commands;
using F1.FeedReplay.Service.Application.Models;

namespace F1.FeedReplay.Service.Application.Contracts;

public interface IReplayCoordinator
{
    Task<ReplayStatusModel> LoadAsync(LoadReplayCommand command, CancellationToken cancellationToken);
    Task<ReplayStatusModel> StartAsync(StartReplayCommand command, CancellationToken cancellationToken);
    Task<ReplayStatusModel> PauseAsync(PauseReplayCommand command, CancellationToken cancellationToken);
    Task<ReplayStatusModel> ResumeAsync(ResumeReplayCommand command, CancellationToken cancellationToken);
    Task<ReplayStatusModel> StopAsync(StopReplayCommand command, CancellationToken cancellationToken);
    Task<ReplayStatusModel> ChangeReplaySpeedAsync(ChangeReplaySpeedCommand command, CancellationToken cancellationToken);
    ReplayStatusModel GetStatus();
}

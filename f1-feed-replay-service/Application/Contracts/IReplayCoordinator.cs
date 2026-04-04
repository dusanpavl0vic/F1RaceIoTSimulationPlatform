using F1.FeedReplay.Service.Application.Models;

namespace F1.FeedReplay.Service.Application.Contracts;

public interface IReplayCoordinator
{
    Task<ReplayStatusModel> LoadAsync(string? configurationPath, CancellationToken cancellationToken);
    Task<ReplayStatusModel> StartAsync(CancellationToken cancellationToken);
    Task<ReplayStatusModel> PauseAsync(CancellationToken cancellationToken);
    Task<ReplayStatusModel> ResumeAsync(CancellationToken cancellationToken);
    Task<ReplayStatusModel> StopAsync(CancellationToken cancellationToken);
    Task<ReplayStatusModel> ChangeReplaySpeedAsync(double speed, CancellationToken cancellationToken);
    ReplayStatusModel GetStatus();
}

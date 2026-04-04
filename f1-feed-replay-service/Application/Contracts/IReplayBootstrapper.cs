using F1.FeedReplay.Service.Application.Models;

namespace F1.FeedReplay.Service.Application.Contracts;

public interface IReplayBootstrapper
{
    Task<ReplayBootstrapResult> BootstrapAsync(ReplayBootstrapParameters parameters, CancellationToken cancellationToken);
}

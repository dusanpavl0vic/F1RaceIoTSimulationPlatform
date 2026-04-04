using F1.FeedReplay.Service.Application.Commands;
using F1.FeedReplay.Service.Application.Models;

namespace F1.FeedReplay.Service.Application.Contracts;

public interface IReplayBootstrapper
{
    Task<ReplayBootstrapResult> BootstrapAsync(BootstrapReplayCommand command, CancellationToken cancellationToken);
}

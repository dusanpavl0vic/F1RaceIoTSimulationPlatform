using F1.FeedReplay.Service.Application.Models;

namespace F1.FeedReplay.Service.Application.Contracts;

public interface IReplayExecutionQueue
{
    ValueTask EnqueueAsync(ReplayExecutionRequest request, CancellationToken cancellationToken);
    ValueTask<ReplayExecutionRequest> DequeueAsync(CancellationToken cancellationToken);
}

using F1.FeedReplay.Service.Domain.Models;

namespace F1.FeedReplay.Service.Application.Contracts;

public interface IRawEventPublisher
{
    Task PublishAsync(string topic, RawReplayEvent rawReplayEvent, CancellationToken cancellationToken);
}

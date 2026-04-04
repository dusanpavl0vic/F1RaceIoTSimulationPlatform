using F1.EventNormalizer.Service.Application.Models;

namespace F1.EventNormalizer.Service.Application.Contracts;

public interface IRawReplayEventSubscriber : IAsyncDisposable
{
    Task ConnectAsync(CancellationToken cancellationToken);
    Task SubscribeAsync(CancellationToken cancellationToken);
    Task<ConsumedRawEvent?> ReadAsync(CancellationToken cancellationToken);
    Task PingAsync(CancellationToken cancellationToken);
}

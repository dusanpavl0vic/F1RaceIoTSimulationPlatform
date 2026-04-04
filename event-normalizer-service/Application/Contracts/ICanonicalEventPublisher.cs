using F1.Shared.Models;

namespace F1.EventNormalizer.Service.Application.Contracts;

public interface ICanonicalEventPublisher : IAsyncDisposable
{
    Task ConnectAsync(CancellationToken cancellationToken);
    Task PublishAsync(string topic, CanonicalEvent canonicalEvent, CancellationToken cancellationToken);
    Task PingAsync(CancellationToken cancellationToken);
}

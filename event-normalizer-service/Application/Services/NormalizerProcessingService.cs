using F1.EventNormalizer.Service.Application.Contracts;
using F1.EventNormalizer.Service.Application.Models;

namespace F1.EventNormalizer.Service.Application.Services;

public sealed class NormalizerProcessingService(
    ICanonicalEventFactory canonicalEventFactory,
    ICanonicalTopicMapper canonicalTopicMapper,
    ICanonicalEventPublisher canonicalEventPublisher,
    NormalizerStatusStore statusStore)
{
    private readonly ICanonicalEventFactory _canonicalEventFactory = canonicalEventFactory;
    private readonly ICanonicalTopicMapper _canonicalTopicMapper = canonicalTopicMapper;
    private readonly ICanonicalEventPublisher _canonicalEventPublisher = canonicalEventPublisher;
    private readonly NormalizerStatusStore _statusStore = statusStore;

    public async Task ProcessAsync(ConsumedRawEvent consumedEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(consumedEvent);

        _statusStore.RecordConsumed(consumedEvent.Topic);

        var canonicalEvents = _canonicalEventFactory.Create(consumedEvent.Event);
        foreach (var canonicalEvent in canonicalEvents)
        {
            var topic = _canonicalTopicMapper.Map(canonicalEvent);
            await _canonicalEventPublisher.PublishAsync(topic, canonicalEvent, cancellationToken);
            _statusStore.RecordPublished(topic, 1);
        }
    }
}

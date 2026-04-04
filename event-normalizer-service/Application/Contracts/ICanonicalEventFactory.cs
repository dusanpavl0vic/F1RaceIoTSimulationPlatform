using F1.Shared.Models;

namespace F1.EventNormalizer.Service.Application.Contracts;

public interface ICanonicalEventFactory
{
    IReadOnlyList<CanonicalEvent> Create(RawReplayEvent rawEvent);
}

using F1.Shared.Models;

namespace F1.EventNormalizer.Service.Application.Contracts;

public interface ICanonicalTopicMapper
{
    string Map(CanonicalEvent canonicalEvent);
}

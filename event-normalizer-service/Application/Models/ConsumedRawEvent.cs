using F1.Shared.Models;

namespace F1.EventNormalizer.Service.Application.Models;

public sealed record ConsumedRawEvent(
    string Topic,
    RawReplayEvent Event);

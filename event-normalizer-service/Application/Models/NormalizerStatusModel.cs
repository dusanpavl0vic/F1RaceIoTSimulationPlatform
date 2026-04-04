namespace F1.EventNormalizer.Service.Application.Models;

public sealed record NormalizerStatusModel(
    long ConsumedRawEvents,
    long PublishedCanonicalEvents,
    string? LastRawTopic,
    string? LastCanonicalTopic,
    string? LastError);

using System.Text.Json.Nodes;

namespace F1.Shared.Models;

public sealed record CanonicalEvent(
    string EventId,
    string SessionId,
    string EventType,
    DateTimeOffset EventTime,
    DateTimeOffset PublishTime,
    long Sequence,
    CanonicalSource Source,
    int? DriverNumber,
    JsonObject Payload);

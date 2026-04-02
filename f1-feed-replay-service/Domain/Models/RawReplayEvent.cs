using System.Text.Json.Nodes;

namespace F1.FeedReplay.Service.Domain.Models;

public sealed record RawReplayEvent(
    string SessionId,
    string SourceFeed,
    string DeviceId,
    DateTimeOffset EventTime,
    DateTimeOffset PublishTime,
    long Sequence,
    int? DriverNumber,
    JsonObject Payload);

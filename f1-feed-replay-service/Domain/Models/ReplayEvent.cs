using System.Text.Json.Nodes;

namespace F1.FeedReplay.Service.Domain.Models;

public sealed record ReplayEvent(
    string SessionId,
    string SourceFeed,
    string DeviceId,
    DateTimeOffset EventTime,
    long Sequence,
    int StableOrder,
    int? DriverNumber,
    JsonNode Payload)
{
    public TimeSpan OffsetFromStart { get; init; } = TimeSpan.Zero;

    public RawReplayEvent ToRawReplayEvent(DateTimeOffset publishTime)
        => new(
            SessionId,
            SourceFeed,
            DeviceId,
            EventTime,
            publishTime,
            Sequence,
            DriverNumber,
            new JsonObject
            {
                ["rawData"] = Payload.DeepClone()
            });
}

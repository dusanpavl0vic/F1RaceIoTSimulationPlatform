using System.Text.Json.Nodes;

namespace F1.RaceState.Service.Domain.Models;

public sealed class RaceStateSnapshot
{
    public string? SessionId { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastProcessedEventTime { get; set; }
    public long? LastProcessedSequence { get; set; }
    public JsonObject? SessionInfo { get; set; }
    public string? TrackStatusCode { get; set; }
    public string? TrackStatusMessage { get; set; }
    public int? CurrentLap { get; set; }
    public int? TotalLaps { get; set; }
    public JsonObject GlobalFeedState { get; set; } = [];
    public Dictionary<int, DriverRaceState> Drivers { get; set; } = new();
}

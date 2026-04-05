using System.Text.Json.Nodes;

namespace F1.RaceState.Service.Domain.Models;

public sealed class DriverRaceState
{
    public int DriverNumber { get; init; }
    public string? BroadcastName { get; set; }
    public string? FullName { get; set; }
    public string? Tla { get; set; }
    public string? TeamName { get; set; }
    public int? Position { get; set; }
    public int? Line { get; set; }
    public int? GridPosition { get; set; }
    public string? GapToLeader { get; set; }
    public string? IntervalToPositionAhead { get; set; }
    public bool? IsCatchingAhead { get; set; }
    public bool InPit { get; set; }
    public bool PitOut { get; set; }
    public bool Retired { get; set; }
    public bool Stopped { get; set; }
    public int? Status { get; set; }
    public string? BestLapTime { get; set; }
    public string? LastLapTime { get; set; }
    public JsonObject? Sectors { get; set; }
    public JsonObject? Speeds { get; set; }
    public string? TyreCompound { get; set; }
    public bool? TyreIsNew { get; set; }
    public JsonObject? TyreStints { get; set; }
    public int? CurrentStintLapCount { get; set; }
    public JsonArray PitStops { get; set; } = [];
    public JsonObject? CurrentTrackPosition { get; set; }
    public DateTimeOffset? CurrentTrackPositionTimestamp { get; set; }
    public JsonObject? LastPositionPacket { get; set; }
    public int? Rpm { get; set; }
    public int? Speed { get; set; }
    public int? Gear { get; set; }
    public int? Throttle { get; set; }
    public int? Brake { get; set; }
    public int? Drs { get; set; }
    public JsonObject? LastTelemetryPacket { get; set; }
    public JsonArray TeamRadioCaptures { get; set; } = [];
}

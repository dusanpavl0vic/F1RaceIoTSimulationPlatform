namespace F1.TelemetryAnalytics.Service.Domain.Models;

public sealed class AnalyticsDriverState
{
    public int DriverNumber { get; init; }
    public string? Tla { get; set; }
    public string? BroadcastName { get; set; }
    public string? FullName { get; set; }
    public string? TeamName { get; set; }
    public string? TeamColor { get; set; }
    public int? GridPosition { get; set; }
    public int? Position { get; set; }
    public int? Line { get; set; }
    public string? GapToLeader { get; set; }
    public string? IntervalToPositionAhead { get; set; }
    public int? CompletedLaps { get; set; }
    public int? NumberOfPitStops { get; set; }
    public bool InPit { get; set; }
    public bool PitOut { get; set; }
    public bool Retired { get; set; }
    public bool Stopped { get; set; }
    public int? Status { get; set; }
    public string? BestLapTime { get; set; }
    public string? LastLapTime { get; set; }
    public string? Sector1Time { get; set; }
    public string? Sector2Time { get; set; }
    public string? Sector3Time { get; set; }
    public string? CurrentCompound { get; set; }
    public bool? TyreIsNew { get; set; }
    public int? TyreLaps { get; set; }
    public int StintNumber { get; set; }
    public int? CurrentStintStartLap { get; set; }
    public DateTimeOffset LastUpdateTimestamp { get; set; }
    public int NextTelemetrySampleIndex { get; set; }
    public LapAggregateState? CurrentLapAggregate { get; set; }
    public int CurrentTelemetryLapNumber => Math.Max(1, (CompletedLaps ?? 0) + 1);
    public string DisplayName => Tla ?? BroadcastName ?? FullName ?? $"#{DriverNumber}";
}

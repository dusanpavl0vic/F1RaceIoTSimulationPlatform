namespace F1.RaceState.Service.Application.Models;

public sealed record RaceLeaderboardEntryModel(
    int DriverNumber,
    string DriverName,
    string TeamName,
    int? Position,
    int? Line,
    int? GridPosition,
    string GapToLeader,
    string IntervalToPositionAhead,
    string LastLapTime,
    string BestLapTime,
    string TyreCompound,
    bool? TyreIsNew,
    int? CurrentStintLapCount,
    string PitFlag,
    string Status,
    int? Speed,
    int? Gear);

using System.Text.Json.Nodes;

namespace F1.RaceState.Service.Application.Models;

public sealed record RaceLeaderboardEntryModel(
    int DriverNumber,
    string? BroadcastName,
    string? FullName,
    string? Tla,
    string? TeamName,
    string? TeamColor,
    int? Position,
    int? Line,
    int? GridPosition,
    string? GapToLeader,
    string? IntervalToPositionAhead,
    bool? IsCatchingAhead,
    bool InPit,
    bool PitOut,
    bool Retired,
    bool Stopped,
    int? Status,
    string? BestLapTime,
    string? LastLapTime,
    string? TyreCompound,
    bool? TyreIsNew,
    int? CurrentStintLapCount);

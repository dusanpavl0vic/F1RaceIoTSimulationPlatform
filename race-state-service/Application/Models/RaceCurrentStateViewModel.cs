using System.Text.Json.Nodes;

namespace F1.RaceState.Service.Application.Models;

public sealed record RaceCurrentStateViewModel(
    string? SessionId,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? LastProcessedEventTime,
    long? LastProcessedSequence,
    JsonObject Session,
    JsonObject GlobalFeedState,
    JsonObject Drivers,
    IReadOnlyList<RaceLeaderboardEntryModel> Leaderboard);

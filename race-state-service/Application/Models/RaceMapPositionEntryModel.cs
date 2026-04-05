namespace F1.RaceState.Service.Application.Models;

public sealed record RaceMapPositionEntryModel(
    int DriverNumber,
    string DriverName,
    int? Position,
    string Status,
    int X,
    int Y,
    int Z,
    DateTimeOffset? Timestamp,
    bool IsEstimated);

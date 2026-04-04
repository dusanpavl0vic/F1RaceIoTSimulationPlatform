namespace F1.EventNormalizer.Service.Application.Models;

public sealed record ResolvedPositionCoordinates(
    int? X,
    int? Y,
    int? Z,
    int? RawX,
    int? RawY,
    int? RawZ,
    bool HasRawCoordinates,
    bool IsEstimated);

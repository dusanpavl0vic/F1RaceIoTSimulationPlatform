using System.Collections.Concurrent;
using F1.EventNormalizer.Service.Application.Contracts;
using F1.EventNormalizer.Service.Application.Models;

namespace F1.EventNormalizer.Service.Domain.Services;

public sealed class PositionCoordinateResolver : IPositionCoordinateResolver
{
    private readonly ConcurrentDictionary<string, (int X, int Y, int Z)> _lastKnownCoordinates = new();

    public ResolvedPositionCoordinates Resolve(
        string sessionId,
        int? driverNumber,
        int? rawX,
        int? rawY,
        int? rawZ)
    {
        var hasRawCoordinates = HasUsableCoordinates(rawX, rawY, rawZ);
        if (driverNumber is null)
        {
            return new ResolvedPositionCoordinates(rawX, rawY, rawZ, rawX, rawY, rawZ, hasRawCoordinates, false);
        }

        var cacheKey = $"{sessionId}:{driverNumber.Value}";
        if (hasRawCoordinates)
        {
            _lastKnownCoordinates[cacheKey] = (rawX!.Value, rawY!.Value, rawZ!.Value);
            return new ResolvedPositionCoordinates(rawX, rawY, rawZ, rawX, rawY, rawZ, true, false);
        }

        if (_lastKnownCoordinates.TryGetValue(cacheKey, out var previous))
        {
            return new ResolvedPositionCoordinates(previous.X, previous.Y, previous.Z, rawX, rawY, rawZ, false, true);
        }

        return new ResolvedPositionCoordinates(rawX, rawY, rawZ, rawX, rawY, rawZ, false, false);
    }

    private static bool HasUsableCoordinates(int? x, int? y, int? z)
        => x is not null
           && y is not null
           && z is not null
           && (x.Value != 0 || y.Value != 0 || z.Value != 0);
}

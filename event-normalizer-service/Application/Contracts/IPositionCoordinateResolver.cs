using F1.EventNormalizer.Service.Application.Models;

namespace F1.EventNormalizer.Service.Application.Contracts;

public interface IPositionCoordinateResolver
{
    ResolvedPositionCoordinates Resolve(
        string sessionId,
        int? driverNumber,
        int? rawX,
        int? rawY,
        int? rawZ);
}

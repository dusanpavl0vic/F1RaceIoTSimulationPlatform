using F1.TelemetryAnalytics.Service.Application.Models;

namespace F1.TelemetryAnalytics.Service.Application.Contracts;

public interface IInfluxTelemetryClient
{
    Task<IReadOnlyList<TelemetrySampleDto>> QueryDriverLapTelemetryAsync(
        string sessionId,
        int driverNumber,
        int lapNumber,
        IReadOnlyCollection<string> requestedMetrics,
        CancellationToken cancellationToken);
}

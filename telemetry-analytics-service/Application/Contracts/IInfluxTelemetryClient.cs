using F1.TelemetryAnalytics.Service.Application.Models;
using F1.TelemetryAnalytics.Service.Domain.Models;

namespace F1.TelemetryAnalytics.Service.Application.Contracts;

public interface IInfluxTelemetryClient
{
    Task WriteTelemetrySampleAsync(TelemetrySampleRecord sample, CancellationToken cancellationToken);
    Task<IReadOnlyList<TelemetryPointDto>> QueryLapTelemetryAsync(string sessionId, int driverNumber, int lapNumber, CancellationToken cancellationToken);
    Task<IReadOnlyList<TelemetrySampleDto>> QueryDriverLapTelemetryAsync(
        string sessionId,
        int driverNumber,
        int lapNumber,
        IReadOnlyCollection<string> requestedMetrics,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<TelemetrySampleDto>> QueryDriverTelemetryAsync(
        string sessionId,
        int driverNumber,
        int maxSamples,
        DateTimeOffset? sinceTimestamp,
        IReadOnlyCollection<string> requestedMetrics,
        CancellationToken cancellationToken);
}

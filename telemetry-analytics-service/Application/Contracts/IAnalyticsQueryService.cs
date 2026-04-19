using F1.TelemetryAnalytics.Service.Application.Models;

namespace F1.TelemetryAnalytics.Service.Application.Contracts;

public interface IAnalyticsQueryService
{
    Task<IReadOnlyList<SegmentBucketDto>> GetDriverSegmentBucketsAsync(string sessionId, int driverNumber, int lapNumber, int bucketCount, CancellationToken cancellationToken);
    Task<DriverTelemetryQueryResultDto> GetDriverLapTelemetryAsync(
        string sessionId,
        int driverNumber,
        int lapNumber,
        IReadOnlyCollection<string>? requestedMetrics,
        CancellationToken cancellationToken);
    Task<DriverTelemetryQueryResultDto> GetLatestDriverTelemetryAsync(
        string sessionId,
        int driverNumber,
        int maxSamples,
        DateTimeOffset? sinceTimestamp,
        IReadOnlyCollection<string>? requestedMetrics,
        CancellationToken cancellationToken);
}

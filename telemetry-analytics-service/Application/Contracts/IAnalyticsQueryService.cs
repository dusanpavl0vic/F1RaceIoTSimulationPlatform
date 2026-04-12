using F1.TelemetryAnalytics.Service.Application.Models;

namespace F1.TelemetryAnalytics.Service.Application.Contracts;

public interface IAnalyticsQueryService
{
    Task<(string DriverName, IReadOnlyList<DriverLapSummaryDto> Laps)> GetDriverLapSummariesAsync(string sessionId, int driverNumber, CancellationToken cancellationToken);
    Task<IReadOnlyList<SegmentBucketDto>> GetDriverSegmentBucketsAsync(string sessionId, int driverNumber, int lapNumber, int bucketCount, CancellationToken cancellationToken);
    Task<CompareDriversOnLapDto> CompareDriversOnLapAsync(string sessionId, int leftDriverNumber, int rightDriverNumber, int lapNumber, int bucketCount, CancellationToken cancellationToken);
}

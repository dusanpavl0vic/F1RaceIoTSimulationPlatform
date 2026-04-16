using F1.TelemetryAnalytics.Service.Application.Models;

namespace F1.TelemetryAnalytics.Service.Application.Contracts;

public interface IAnalyticsQueryService
{
    Task<IReadOnlyList<SessionOverviewDto>> ListSessionsAsync(CancellationToken cancellationToken);
    Task<SessionOverviewDto?> GetSessionOverviewAsync(string sessionId, CancellationToken cancellationToken);
    Task<IReadOnlyList<DriverSessionOverviewDto>> GetSessionDriversAsync(string sessionId, CancellationToken cancellationToken);
    Task<(string DriverName, IReadOnlyList<DriverStintDto> Stints)> GetDriverStintsAsync(string sessionId, int driverNumber, CancellationToken cancellationToken);
    Task<(string DriverName, IReadOnlyList<DriverLapSummaryDto> Laps)> GetDriverLapSummariesAsync(string sessionId, int driverNumber, CancellationToken cancellationToken);
    Task<IReadOnlyList<SegmentBucketDto>> GetDriverSegmentBucketsAsync(string sessionId, int driverNumber, int lapNumber, int bucketCount, CancellationToken cancellationToken);
    Task<CompareDriversOnLapDto> CompareDriversOnLapAsync(string sessionId, int leftDriverNumber, int rightDriverNumber, int lapNumber, int bucketCount, CancellationToken cancellationToken);
    Task<IReadOnlyList<TelemetryPointDto>> GetLatestDriverTelemetryAsync(string sessionId, int driverNumber, int maxSamples, CancellationToken cancellationToken);
}

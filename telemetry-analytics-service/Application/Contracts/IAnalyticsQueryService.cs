using F1.TelemetryAnalytics.Service.Application.Models;

namespace F1.TelemetryAnalytics.Service.Application.Contracts;

public interface IAnalyticsQueryService
{
    Task<TyreStintStrategyDto> GetTyreStintStrategyAsync(string sessionId, CancellationToken cancellationToken);
    Task<DriverTelemetryQueryResultDto> GetDriverLapTelemetryAsync(
        string sessionId,
        int driverNumber,
        int lapNumber,
        IReadOnlyCollection<string>? requestedMetrics,
        CancellationToken cancellationToken);
}

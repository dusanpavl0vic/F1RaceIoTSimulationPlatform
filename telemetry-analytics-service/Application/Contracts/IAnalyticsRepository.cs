using F1.TelemetryAnalytics.Service.Application.Models;
using F1.TelemetryAnalytics.Service.Domain.Models;

namespace F1.TelemetryAnalytics.Service.Application.Contracts;

public interface IAnalyticsRepository
{
    Task InitializeAsync(CancellationToken cancellationToken);
    Task UpsertSessionAsync(AnalyticsSessionState session, CancellationToken cancellationToken);
    Task UpsertDriverAsync(string sessionId, AnalyticsDriverState driver, CancellationToken cancellationToken);
    Task UpsertLapSummaryAsync(LapSummaryRecord lapSummary, CancellationToken cancellationToken);
    Task UpsertStintSummaryAsync(StintSummaryRecord stintSummary, CancellationToken cancellationToken);
    Task<(string DriverName, IReadOnlyList<DriverLapSummaryDto> Laps)> GetDriverLapSummariesAsync(string sessionId, int driverNumber, CancellationToken cancellationToken);
}

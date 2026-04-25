using F1.TelemetryAnalytics.Service.Application.Models;
namespace F1.TelemetryAnalytics.Service.Application.Contracts;

public interface IAnalyticsRepository
{
    Task InitializeAsync(CancellationToken cancellationToken);
    Task<TyreStintStrategyDto> GetTyreStintStrategyAsync(string sessionId, CancellationToken cancellationToken);
}

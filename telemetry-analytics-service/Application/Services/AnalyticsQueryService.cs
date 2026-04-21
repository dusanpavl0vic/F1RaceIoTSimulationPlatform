using F1.TelemetryAnalytics.Service.Application.Contracts;
using F1.TelemetryAnalytics.Service.Application.Models;

namespace F1.TelemetryAnalytics.Service.Application.Services;

public sealed class AnalyticsQueryService(
    IInfluxTelemetryClient influxTelemetryClient,
    IAnalyticsRepository analyticsRepository) : IAnalyticsQueryService
{
    private readonly IInfluxTelemetryClient _influxTelemetryClient = influxTelemetryClient;
    private readonly IAnalyticsRepository _analyticsRepository = analyticsRepository;

    public Task<TyreStintStrategyDto> GetTyreStintStrategyAsync(
        string sessionId,
        CancellationToken cancellationToken)
        => _analyticsRepository.GetTyreStintStrategyAsync(sessionId, cancellationToken);

    public async Task<DriverTelemetryQueryResultDto> GetDriverLapTelemetryAsync(
        string sessionId,
        int driverNumber,
        int lapNumber,
        IReadOnlyCollection<string>? requestedMetrics,
        CancellationToken cancellationToken)
    {
        var metrics = TelemetryMetricCatalog.NormalizeRequestedMetrics(requestedMetrics);
        var samples = await _influxTelemetryClient.QueryDriverLapTelemetryAsync(
            sessionId,
            driverNumber,
            lapNumber,
            metrics,
            cancellationToken);

        return new DriverTelemetryQueryResultDto(metrics, samples);
    }
}

using F1.TelemetryAnalytics.Service.Application.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace F1.TelemetryAnalytics.Service.API.Controllers;

[ApiController]
[Route("api/telemetry-analytics")]
public sealed class TelemetryAnalyticsController(IAnalyticsQueryService analyticsQueryService) : ControllerBase
{
    private readonly IAnalyticsQueryService _analyticsQueryService = analyticsQueryService;

    [HttpGet("tyres/stints")]
    public async Task<IActionResult> GetTyreStintStrategy(
        [FromQuery] string sessionId,
        CancellationToken cancellationToken = default)
        => Ok(await _analyticsQueryService.GetTyreStintStrategyAsync(sessionId, cancellationToken));

    [HttpGet("drivers/{driverNumber:int}/laps/{lapNumber:int}/telemetry")]
    public async Task<IActionResult> GetDriverLapTelemetry(
        [FromRoute] int driverNumber,
        [FromRoute] int lapNumber,
        [FromQuery] string sessionId,
        [FromQuery] string[]? metrics = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _analyticsQueryService.GetDriverLapTelemetryAsync(
            sessionId,
            driverNumber,
            lapNumber,
            ExpandMetrics(metrics),
            cancellationToken);

        return Ok(new
        {
            sessionId,
            driverNumber,
            lapNumber,
            metrics = result.Metrics,
            samples = result.Samples
        });
    }

    private static string[] ExpandMetrics(string[]? metrics)
        => (metrics ?? [])
            .SelectMany(metric => metric.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .ToArray();
}

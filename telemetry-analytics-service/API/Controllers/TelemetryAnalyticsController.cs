using F1.TelemetryAnalytics.Service.Application.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace F1.TelemetryAnalytics.Service.API.Controllers;

[ApiController]
[Route("api/telemetry-analytics")]
public sealed class TelemetryAnalyticsController(IAnalyticsQueryService analyticsQueryService) : ControllerBase
{
    private readonly IAnalyticsQueryService _analyticsQueryService = analyticsQueryService;

    [HttpGet("drivers/{driverNumber:int}/segments")]
    public async Task<IActionResult> GetDriverSegmentBuckets(
        [FromRoute] int driverNumber,
        [FromQuery] string sessionId,
        [FromQuery] int lapNumber,
        [FromQuery] int bucketCount = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _analyticsQueryService.GetDriverSegmentBucketsAsync(
            sessionId,
            driverNumber,
            lapNumber,
            bucketCount,
            cancellationToken);

        return Ok(new
        {
            sessionId,
            driverNumber,
            lapNumber,
            bucketCount,
            buckets = result
        });
    }

    [HttpGet("drivers/{driverNumber:int}/telemetry")]
    public async Task<IActionResult> GetLatestDriverTelemetry(
        [FromRoute] int driverNumber,
        [FromQuery] string sessionId,
        [FromQuery] int maxSamples = 200,
        [FromQuery] DateTimeOffset? sinceTimestamp = null,
        [FromQuery] string[]? metrics = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _analyticsQueryService.GetLatestDriverTelemetryAsync(
            sessionId,
            driverNumber,
            maxSamples,
            sinceTimestamp,
            ExpandMetrics(metrics),
            cancellationToken);

        return Ok(new
        {
            sessionId,
            driverNumber,
            sinceTimestamp,
            metrics = result.Metrics,
            samples = result.Samples
        });
    }

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

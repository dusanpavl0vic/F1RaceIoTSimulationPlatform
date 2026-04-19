using F1.RaceState.Service.Application.Queries;
using Microsoft.AspNetCore.Mvc;

namespace F1.RaceState.Service.API.Controllers;

[ApiController]
[Route("api/race-state/analytics")]
public sealed class RaceStateAnalyticsController(RaceStateAnalyticsQueryHandler queryHandler) : ControllerBase
{
    [HttpGet("drivers/{driverNumber:int}/segments")]
    public async Task<IActionResult> GetDriverSegmentBuckets(
        [FromRoute] int driverNumber,
        [FromQuery] string? sessionId,
        [FromQuery] int lapNumber,
        [FromQuery] int bucketCount = 10,
        CancellationToken cancellationToken = default)
    {
        var resolvedSessionId = queryHandler.Handle(new ResolveAnalyticsSessionIdQuery(sessionId));
        if (resolvedSessionId is null)
        {
            return BadRequest("sessionId is required.");
        }

        return Ok(await queryHandler.HandleAsync(
            new GetDriverSegmentBucketsQuery(
                resolvedSessionId,
                driverNumber,
                lapNumber,
                bucketCount),
            cancellationToken));
    }

    [HttpGet("drivers/{driverNumber:int}/telemetry")]
    public async Task<IActionResult> GetLatestDriverTelemetry(
        [FromRoute] int driverNumber,
        [FromQuery] string? sessionId,
        [FromQuery] int maxSamples = 0,
        [FromQuery] DateTimeOffset? sinceTimestamp = null,
        [FromQuery] string[]? metrics = null,
        CancellationToken cancellationToken = default)
    {
        var resolvedSessionId = queryHandler.Handle(new ResolveAnalyticsSessionIdQuery(sessionId));
        if (resolvedSessionId is null)
        {
            return BadRequest("sessionId is required.");
        }

        return Ok(await queryHandler.HandleAsync(
            new GetLatestDriverTelemetryQuery(
                resolvedSessionId,
                driverNumber,
                maxSamples,
                sinceTimestamp,
                ExpandMetrics(metrics)),
            cancellationToken));
    }

    [HttpGet("drivers/{driverNumber:int}/laps/{lapNumber:int}/telemetry")]
    public async Task<IActionResult> GetDriverLapTelemetry(
        [FromRoute] int driverNumber,
        [FromRoute] int lapNumber,
        [FromQuery] string? sessionId,
        [FromQuery] string[]? metrics = null,
        CancellationToken cancellationToken = default)
    {
        var resolvedSessionId = queryHandler.Handle(new ResolveAnalyticsSessionIdQuery(sessionId));
        if (resolvedSessionId is null)
        {
            return BadRequest("sessionId is required.");
        }

        return Ok(await queryHandler.HandleAsync(
            new GetDriverLapTelemetryQuery(
                resolvedSessionId,
                driverNumber,
                lapNumber,
                ExpandMetrics(metrics)),
            cancellationToken));
    }

    private static string[] ExpandMetrics(string[]? metrics)
        => (metrics ?? [])
            .SelectMany(metric => metric.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .ToArray();
}

using F1.RaceState.Service.Application.Queries;
using Microsoft.AspNetCore.Mvc;

namespace F1.RaceState.Service.API.Controllers;

[ApiController]
[Route("api/race-state/analytics")]
public sealed class RaceStateAnalyticsController(RaceStateAnalyticsQueryHandler queryHandler) : ControllerBase
{
    [HttpGet("tyres/stints")]
    public async Task<IActionResult> GetTyreStintStrategy(
        [FromQuery] string? sessionId,
        CancellationToken cancellationToken = default)
    {
        var resolvedSessionId = queryHandler.Handle(new ResolveAnalyticsSessionIdQuery(sessionId));
        if (resolvedSessionId is null)
        {
            return BadRequest("sessionId is required.");
        }

        return Ok(await queryHandler.HandleAsync(
            new GetTyreStintStrategyQuery(resolvedSessionId),
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

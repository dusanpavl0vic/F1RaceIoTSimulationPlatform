using F1.RaceState.Service.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace F1.RaceState.Service.API.Controllers;

[ApiController]
[Route("api/race-state/analytics")]
public sealed class RaceStateAnalyticsController(RaceStateAnalyticsService raceStateAnalyticsService) : ControllerBase
{

    [HttpGet("sessions")]
    public async Task<IActionResult> GetSessions(CancellationToken cancellationToken)
    {
        return Ok(await raceStateAnalyticsService.ListSessionsAsync(cancellationToken));
    }

    [HttpGet("sessions/{sessionId}")]
    public async Task<IActionResult> GetSessionOverview([FromRoute] string sessionId, CancellationToken cancellationToken)
    {
        var resolvedSessionId = raceStateAnalyticsService.ResolveSessionId(sessionId);
        if (resolvedSessionId is null)
        {
            return BadRequest("sessionId is required.");
        }

        var result = await raceStateAnalyticsService.GetSessionOverviewAsync(resolvedSessionId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("sessions/{sessionId}/drivers")]
    public async Task<IActionResult> GetSessionDrivers([FromRoute] string sessionId, CancellationToken cancellationToken)
    {
        var resolvedSessionId = raceStateAnalyticsService.ResolveSessionId(sessionId);
        if (resolvedSessionId is null)
        {
            return BadRequest("sessionId is required.");
        }

        return Ok(await raceStateAnalyticsService.GetSessionDriversAsync(resolvedSessionId, cancellationToken));
    }

    [HttpGet("drivers/{driverNumber:int}/stints")]
    public async Task<IActionResult> GetDriverStints([FromRoute] int driverNumber, [FromQuery] string? sessionId, CancellationToken cancellationToken)
    {
        var resolvedSessionId = raceStateAnalyticsService.ResolveSessionId(sessionId);
        if (resolvedSessionId is null)
        {
            return BadRequest("sessionId is required.");
        }

        return Ok(await raceStateAnalyticsService.GetDriverStintsAsync(resolvedSessionId, driverNumber, cancellationToken));
    }

    [HttpGet("drivers/{driverNumber:int}/laps")]
    public async Task<IActionResult> GetDriverLapSummaries([FromRoute] int driverNumber, [FromQuery] string? sessionId, CancellationToken cancellationToken)
    {
        var resolvedSessionId = raceStateAnalyticsService.ResolveSessionId(sessionId);
        if (resolvedSessionId is null)
        {
            return BadRequest("sessionId is required.");
        }

        return Ok(await raceStateAnalyticsService.GetDriverLapSummariesAsync(resolvedSessionId, driverNumber, cancellationToken));
    }

    [HttpGet("drivers/{driverNumber:int}/segments")]
    public async Task<IActionResult> GetDriverSegmentBuckets(
        [FromRoute] int driverNumber,
        [FromQuery] string? sessionId,
        [FromQuery] int lapNumber,
        [FromQuery] int bucketCount = 10,
        CancellationToken cancellationToken = default)
    {
        var resolvedSessionId = raceStateAnalyticsService.ResolveSessionId(sessionId);
        if (resolvedSessionId is null)
        {
            return BadRequest("sessionId is required.");
        }

        return Ok(await raceStateAnalyticsService.GetDriverSegmentBucketsAsync(
            resolvedSessionId,
            driverNumber,
            lapNumber,
            bucketCount,
            cancellationToken));
    }

    [HttpGet("drivers/{driverNumber:int}/telemetry")]
    public async Task<IActionResult> GetLatestDriverTelemetry(
        [FromRoute] int driverNumber,
        [FromQuery] string? sessionId,
        [FromQuery] int maxSamples = 200,
        CancellationToken cancellationToken = default)
    {
        var resolvedSessionId = raceStateAnalyticsService.ResolveSessionId(sessionId);
        if (resolvedSessionId is null)
        {
            return BadRequest("sessionId is required.");
        }

        return Ok(await raceStateAnalyticsService.GetLatestDriverTelemetryAsync(
            resolvedSessionId,
            driverNumber,
            maxSamples,
            cancellationToken));
    }

    [HttpGet("compare")]
    public async Task<IActionResult> CompareDriversOnLap(
        [FromQuery] string? sessionId,
        [FromQuery] int leftDriverNumber,
        [FromQuery] int rightDriverNumber,
        [FromQuery] int lapNumber,
        [FromQuery] int bucketCount = 20,
        CancellationToken cancellationToken = default)
    {
        var resolvedSessionId = raceStateAnalyticsService.ResolveSessionId(sessionId);
        if (resolvedSessionId is null)
        {
            return BadRequest("sessionId is required.");
        }

        return Ok(await raceStateAnalyticsService.CompareDriversOnLapAsync(
            resolvedSessionId,
            leftDriverNumber,
            rightDriverNumber,
            lapNumber,
            bucketCount,
            cancellationToken));
    }
}

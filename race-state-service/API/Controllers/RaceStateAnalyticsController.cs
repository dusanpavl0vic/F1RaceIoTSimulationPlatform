using F1.RaceState.Service.Application.Contracts;
using F1.RaceState.Service.Infrastructure.Grpc;
using Microsoft.AspNetCore.Mvc;

namespace F1.RaceState.Service.API.Controllers;

[ApiController]
[Route("api/race-state/analytics")]
public sealed class RaceStateAnalyticsController(
    TelemetryAnalyticsGateway telemetryAnalyticsGateway,
    IRaceStateStore raceStateStore) : ControllerBase
{
    private readonly TelemetryAnalyticsGateway _telemetryAnalyticsGateway = telemetryAnalyticsGateway;
    private readonly IRaceStateStore _raceStateStore = raceStateStore;

    [HttpGet("sessions")]
    public async Task<IActionResult> GetSessions(CancellationToken cancellationToken)
    {
        var response = await _telemetryAnalyticsGateway.ListSessionsAsync(cancellationToken);
        return Ok(new { sessions = response.Sessions });
    }

    [HttpGet("sessions/{sessionId}")]
    public async Task<IActionResult> GetSessionOverview([FromRoute] string sessionId, CancellationToken cancellationToken)
    {
        var resolvedSessionId = ResolveSessionId(sessionId);
        if (resolvedSessionId is null)
        {
            return BadRequest("sessionId is required.");
        }

        var response = await _telemetryAnalyticsGateway.GetSessionOverviewAsync(resolvedSessionId, cancellationToken);
        return response.Session is null ? NotFound() : Ok(response.Session);
    }

    [HttpGet("sessions/{sessionId}/drivers")]
    public async Task<IActionResult> GetSessionDrivers([FromRoute] string sessionId, CancellationToken cancellationToken)
    {
        var resolvedSessionId = ResolveSessionId(sessionId);
        if (resolvedSessionId is null)
        {
            return BadRequest("sessionId is required.");
        }

        var response = await _telemetryAnalyticsGateway.GetSessionDriversAsync(resolvedSessionId, cancellationToken);
        return Ok(new
        {
            sessionId = resolvedSessionId,
            drivers = response.Drivers
        });
    }

    [HttpGet("drivers/{driverNumber:int}/stints")]
    public async Task<IActionResult> GetDriverStints([FromRoute] int driverNumber, [FromQuery] string? sessionId, CancellationToken cancellationToken)
    {
        var resolvedSessionId = ResolveSessionId(sessionId);
        if (resolvedSessionId is null)
        {
            return BadRequest("sessionId is required.");
        }

        var response = await _telemetryAnalyticsGateway.GetDriverStintsAsync(resolvedSessionId, driverNumber, cancellationToken);
        return Ok(new
        {
            sessionId = resolvedSessionId,
            driverNumber,
            driverName = response.DriverName,
            stints = response.Stints
        });
    }

    [HttpGet("drivers/{driverNumber:int}/laps")]
    public async Task<IActionResult> GetDriverLapSummaries([FromRoute] int driverNumber, [FromQuery] string? sessionId, CancellationToken cancellationToken)
    {
        var resolvedSessionId = ResolveSessionId(sessionId);
        if (resolvedSessionId is null)
        {
            return BadRequest("sessionId is required.");
        }

        var response = await _telemetryAnalyticsGateway.GetDriverLapSummariesAsync(resolvedSessionId, driverNumber, cancellationToken);
        return Ok(new
        {
            sessionId = resolvedSessionId,
            driverNumber,
            driverName = response.DriverName,
            laps = response.Laps
        });
    }

    [HttpGet("drivers/{driverNumber:int}/segments")]
    public async Task<IActionResult> GetDriverSegmentBuckets(
        [FromRoute] int driverNumber,
        [FromQuery] string? sessionId,
        [FromQuery] int lapNumber,
        [FromQuery] int bucketCount = 10,
        CancellationToken cancellationToken = default)
    {
        var resolvedSessionId = ResolveSessionId(sessionId);
        if (resolvedSessionId is null)
        {
            return BadRequest("sessionId is required.");
        }

        var response = await _telemetryAnalyticsGateway.GetDriverSegmentBucketsAsync(
            resolvedSessionId,
            driverNumber,
            lapNumber,
            bucketCount,
            cancellationToken);

        return Ok(new
        {
            sessionId = resolvedSessionId,
            driverNumber,
            lapNumber,
            bucketCount,
            buckets = response.Buckets
        });
    }

    [HttpGet("drivers/{driverNumber:int}/telemetry")]
    public async Task<IActionResult> GetLatestDriverTelemetry(
        [FromRoute] int driverNumber,
        [FromQuery] string? sessionId,
        [FromQuery] int maxSamples = 200,
        CancellationToken cancellationToken = default)
    {
        var resolvedSessionId = ResolveSessionId(sessionId);
        if (resolvedSessionId is null)
        {
            return BadRequest("sessionId is required.");
        }

        var response = await _telemetryAnalyticsGateway.GetLatestDriverTelemetryAsync(
            resolvedSessionId,
            driverNumber,
            maxSamples,
            cancellationToken);

        return Ok(new
        {
            sessionId = resolvedSessionId,
            driverNumber,
            samples = response.Samples
        });
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
        var resolvedSessionId = ResolveSessionId(sessionId);
        if (resolvedSessionId is null)
        {
            return BadRequest("sessionId is required.");
        }

        var response = await _telemetryAnalyticsGateway.CompareDriversOnLapAsync(
            resolvedSessionId,
            leftDriverNumber,
            rightDriverNumber,
            lapNumber,
            bucketCount,
            cancellationToken);

        return Ok(response);
    }

    private string? ResolveSessionId(string? requestedSessionId)
    {
        if (!string.IsNullOrWhiteSpace(requestedSessionId))
        {
            return requestedSessionId;
        }

        return _raceStateStore.GetSnapshot().SessionId;
    }
}

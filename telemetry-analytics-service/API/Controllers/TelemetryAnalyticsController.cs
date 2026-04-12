using F1.TelemetryAnalytics.Service.Application.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace F1.TelemetryAnalytics.Service.API.Controllers;

[ApiController]
[Route("api/telemetry-analytics")]
public sealed class TelemetryAnalyticsController(IAnalyticsQueryService analyticsQueryService) : ControllerBase
{
    private readonly IAnalyticsQueryService _analyticsQueryService = analyticsQueryService;

    [HttpGet("drivers/{driverNumber:int}/laps")]
    public async Task<IActionResult> GetDriverLapSummaries([FromRoute] int driverNumber, [FromQuery] string sessionId, CancellationToken cancellationToken)
    {
        var result = await _analyticsQueryService.GetDriverLapSummariesAsync(sessionId, driverNumber, cancellationToken);
        return Ok(new
        {
            sessionId,
            driverNumber,
            driverName = result.DriverName,
            laps = result.Laps
        });
    }

    [HttpGet("drivers/{driverNumber:int}/segments")]
    public async Task<IActionResult> GetDriverSegmentBuckets(
        [FromRoute] int driverNumber,
        [FromQuery] string sessionId,
        [FromQuery] int lapNumber,
        [FromQuery] int bucketCount = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _analyticsQueryService.GetDriverSegmentBucketsAsync(sessionId, driverNumber, lapNumber, bucketCount, cancellationToken);
        return Ok(new
        {
            sessionId,
            driverNumber,
            lapNumber,
            bucketCount,
            buckets = result
        });
    }

    [HttpGet("compare")]
    public async Task<IActionResult> CompareDriversOnLap(
        [FromQuery] string sessionId,
        [FromQuery] int leftDriverNumber,
        [FromQuery] int rightDriverNumber,
        [FromQuery] int lapNumber,
        [FromQuery] int bucketCount = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _analyticsQueryService.CompareDriversOnLapAsync(
            sessionId,
            leftDriverNumber,
            rightDriverNumber,
            lapNumber,
            bucketCount,
            cancellationToken);

        return Ok(result);
    }
}

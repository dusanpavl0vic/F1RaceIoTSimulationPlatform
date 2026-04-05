using F1.RaceState.Service.Application.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace F1.RaceState.Service.API.Controllers;

[ApiController]
[Route("api/race-state")]
public sealed class RaceStateController(IRaceStateStore raceStateStore) : ControllerBase
{
    [HttpGet("current")]
    public IActionResult GetCurrent() => Ok(raceStateStore.GetSnapshot());

    [HttpGet("recovery")]
    public IActionResult GetRecoveryState()
    {
        var checkpoint = raceStateStore.GetCheckpoint();
        return Ok(new
        {
            checkpoint.Snapshot.SessionId,
            checkpoint.Snapshot.UpdatedAt,
            checkpoint.Snapshot.LastProcessedEventTime,
            checkpoint.Snapshot.LastProcessedSequence,
            DriverCount = checkpoint.Snapshot.Drivers.Count,
            VersionKeyCount = checkpoint.LastAppliedEventVersions.Count
        });
    }
}

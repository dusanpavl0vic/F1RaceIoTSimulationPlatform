using F1.RaceState.Service.Application.Contracts;
using F1.RaceState.Service.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace F1.RaceState.Service.API.Controllers;

[ApiController]
[Route("api/race-state")]
public sealed class RaceStateController(IRaceStateStore raceStateStore, RaceStateViewFactory viewFactory) : ControllerBase
{
    [HttpGet("current")]
    public IActionResult GetCurrent()
    {
        var snapshot = raceStateStore.GetSnapshot();
        return Ok(viewFactory.BuildCurrentState(snapshot));
    }

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

    [HttpGet("drivers")]
    public IActionResult GetDrivers()
    {
        var snapshot = raceStateStore.GetSnapshot();
        return Ok(viewFactory.BuildLeaderboard(snapshot));
    }

    [HttpGet("leaderboard")]
    public IActionResult GetLeaderboard()
    {
        var snapshot = raceStateStore.GetSnapshot();
        return Ok(new
        {
            snapshot.SessionId,
            snapshot.CurrentLap,
            snapshot.TotalLaps,
            snapshot.TrackStatusCode,
            snapshot.TrackStatusMessage,
            Drivers = viewFactory.BuildLeaderboard(snapshot)
        });
    }

    [HttpGet("map")]
    public IActionResult GetMap()
    {
        var snapshot = raceStateStore.GetSnapshot();
        return Ok(new
        {
            snapshot.SessionId,
            Positions = viewFactory.BuildMapPositions(snapshot)
        });
    }

    [HttpGet("dashboard")]
    public IActionResult GetDashboard()
    {
        var snapshot = raceStateStore.GetSnapshot();
        return Ok(viewFactory.BuildDashboard(snapshot));
    }
}

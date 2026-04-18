using F1.RaceState.Service.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace F1.RaceState.Service.API.Controllers;

[ApiController]
[Route("api/race-state")]
public sealed class RaceStateController(RaceStateReadService raceStateReadService) : ControllerBase
{
    [HttpGet("current")]
    public IActionResult GetCurrent()
    {
        return Ok(raceStateReadService.GetCurrent());
    }

    [HttpGet("recovery")]
    public IActionResult GetRecoveryState()
    {
        return Ok(raceStateReadService.GetRecoveryState());
    }

    [HttpGet("drivers")]
    public IActionResult GetDrivers()
    {
        return Ok(raceStateReadService.GetDrivers());
    }

    [HttpGet("leaderboard")]
    public IActionResult GetLeaderboard()
    {
        return Ok(raceStateReadService.GetLeaderboard());
    }

    [HttpGet("map")]
    public IActionResult GetMap()
    {
        return Ok(raceStateReadService.GetMap());
    }

    [HttpGet("dashboard")]
    public IActionResult GetDashboard()
    {
        return Ok(raceStateReadService.GetDashboard());
    }
}

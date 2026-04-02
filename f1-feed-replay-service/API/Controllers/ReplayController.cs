using F1.FeedReplay.Service.API.Contracts;
using F1.FeedReplay.Service.Application.Commands;
using F1.FeedReplay.Service.Application.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace F1.FeedReplay.Service.API.Controllers;

[ApiController]
[Route("api/replay")]
public sealed class ReplayController(IReplayCoordinator replayCoordinator) : ControllerBase
{
    [HttpPost("load")]
    public async Task<IActionResult> LoadAsync([FromBody] LoadReplayRequest request, CancellationToken cancellationToken)
    {
        var status = await replayCoordinator.LoadAsync(new LoadReplayCommand(request.ConfigurationPath ?? string.Empty), cancellationToken);
        return Ok(status);
    }

    [HttpPost("start")]
    public async Task<IActionResult> StartAsync(CancellationToken cancellationToken)
    {
        var status = await replayCoordinator.StartAsync(new StartReplayCommand(), cancellationToken);
        return Ok(status);
    }

    [HttpPost("pause")]
    public async Task<IActionResult> PauseAsync(CancellationToken cancellationToken)
    {
        var status = await replayCoordinator.PauseAsync(new PauseReplayCommand(), cancellationToken);
        return Ok(status);
    }

    [HttpPost("resume")]
    public async Task<IActionResult> ResumeAsync(CancellationToken cancellationToken)
    {
        var status = await replayCoordinator.ResumeAsync(new ResumeReplayCommand(), cancellationToken);
        return Ok(status);
    }

    [HttpPost("stop")]
    public async Task<IActionResult> StopAsync(CancellationToken cancellationToken)
    {
        var status = await replayCoordinator.StopAsync(new StopReplayCommand(), cancellationToken);
        return Ok(status);
    }

    [HttpPost("speed")]
    public async Task<IActionResult> ChangeSpeedAsync([FromBody] ChangeReplaySpeedRequest request, CancellationToken cancellationToken)
    {
        var status = await replayCoordinator.ChangeReplaySpeedAsync(new ChangeReplaySpeedCommand(request.Speed), cancellationToken);
        return Ok(status);
    }

    [HttpGet("status")]
    public ActionResult GetStatus()
    {
        return Ok(replayCoordinator.GetStatus());
    }
}

using F1.FeedReplay.Service.API.Contracts;
using F1.FeedReplay.Service.API.Mappers;
using F1.FeedReplay.Service.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace F1.FeedReplay.Service.API.Controllers;

[ApiController]
[Route("api/replay")]
public sealed class ReplayController(
    ReplayApplicationService replayApplicationService) : ControllerBase
{
    [HttpPost("bootstrap")]
    public async Task<IActionResult> BootstrapAsync([FromBody] ReplayBootstrapRequest request, CancellationToken cancellationToken)
    {
        var result = await replayApplicationService.BootstrapAsync(ReplayRequestMapper.Map(request), cancellationToken);

        return Ok(result);
    }

    [HttpPost("load")]
    public async Task<IActionResult> LoadAsync([FromBody] LoadReplayRequest request, CancellationToken cancellationToken)
    {
        var status = await replayApplicationService.LoadAsync(ReplayRequestMapper.Map(request), cancellationToken);
        return Ok(status);
    }

    [HttpPost("start")]
    public async Task<IActionResult> StartAsync(CancellationToken cancellationToken)
    {
        var status = await replayApplicationService.StartAsync(cancellationToken);
        return Ok(status);
    }

    [HttpPost("pause")]
    public async Task<IActionResult> PauseAsync(CancellationToken cancellationToken)
    {
        var status = await replayApplicationService.PauseAsync(cancellationToken);
        return Ok(status);
    }

    [HttpPost("resume")]
    public async Task<IActionResult> ResumeAsync(CancellationToken cancellationToken)
    {
        var status = await replayApplicationService.ResumeAsync(cancellationToken);
        return Ok(status);
    }

    [HttpPost("stop")]
    public async Task<IActionResult> StopAsync(CancellationToken cancellationToken)
    {
        var status = await replayApplicationService.StopAsync(cancellationToken);
        return Ok(status);
    }

    [HttpPost("speed")]
    public async Task<IActionResult> ChangeSpeedAsync([FromBody] ChangeReplaySpeedRequest request, CancellationToken cancellationToken)
    {
        var status = await replayApplicationService.ChangeReplaySpeedAsync(ReplayRequestMapper.Map(request), cancellationToken);
        return Ok(status);
    }

    [HttpGet("status")]
    public ActionResult GetStatus()
    {
        return Ok(replayApplicationService.GetStatus());
    }
}

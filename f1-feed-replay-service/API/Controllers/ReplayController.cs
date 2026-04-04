using F1.FeedReplay.Service.API.Contracts;
using F1.FeedReplay.Service.Application.Contracts;
using F1.FeedReplay.Service.Application.Models;
using Microsoft.AspNetCore.Mvc;

namespace F1.FeedReplay.Service.API.Controllers;

[ApiController]
[Route("api/replay")]
public sealed class ReplayController(
    IReplayCoordinator replayCoordinator,
    IReplayBootstrapper replayBootstrapper) : ControllerBase
{
    [HttpPost("bootstrap")]
    public async Task<IActionResult> BootstrapAsync([FromBody] ReplayBootstrapRequest request, CancellationToken cancellationToken)
    {
        var result = await replayBootstrapper.BootstrapAsync(
            new ReplayBootstrapParameters(
                request.IndexUrl,
                request.ConfigurationPath,
                request.SessionId,
                request.FeedNames,
                request.DownloadFeeds,
                request.ForceDownload,
                request.LoadAfterDownload,
                request.StartAfterLoad),
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("load")]
    public async Task<IActionResult> LoadAsync([FromBody] LoadReplayRequest request, CancellationToken cancellationToken)
    {
        var status = await replayCoordinator.LoadAsync(request.ConfigurationPath, cancellationToken);
        return Ok(status);
    }

    [HttpPost("start")]
    public async Task<IActionResult> StartAsync(CancellationToken cancellationToken)
    {
        var status = await replayCoordinator.StartAsync(cancellationToken);
        return Ok(status);
    }

    [HttpPost("pause")]
    public async Task<IActionResult> PauseAsync(CancellationToken cancellationToken)
    {
        var status = await replayCoordinator.PauseAsync(cancellationToken);
        return Ok(status);
    }

    [HttpPost("resume")]
    public async Task<IActionResult> ResumeAsync(CancellationToken cancellationToken)
    {
        var status = await replayCoordinator.ResumeAsync(cancellationToken);
        return Ok(status);
    }

    [HttpPost("stop")]
    public async Task<IActionResult> StopAsync(CancellationToken cancellationToken)
    {
        var status = await replayCoordinator.StopAsync(cancellationToken);
        return Ok(status);
    }

    [HttpPost("speed")]
    public async Task<IActionResult> ChangeSpeedAsync([FromBody] ChangeReplaySpeedRequest request, CancellationToken cancellationToken)
    {
        var status = await replayCoordinator.ChangeReplaySpeedAsync(request.Speed, cancellationToken);
        return Ok(status);
    }

    [HttpGet("status")]
    public ActionResult GetStatus()
    {
        return Ok(replayCoordinator.GetStatus());
    }
}

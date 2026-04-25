using F1.RaceState.Service.Application.Models;
using F1.RaceState.Service.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace F1.RaceState.Service.API.Controllers;

[ApiController]
[Route("api/race-state/alerts")]
public sealed class RaceStateAlertsController(RaceBattleAlertService raceBattleAlertService) : ControllerBase
{
    [HttpPost("battle")]
    public async Task<IActionResult> PublishBattleAlert(
        [FromBody] BattleAlertCandidateRequest candidate,
        CancellationToken cancellationToken)
    {
        var alert = await raceBattleAlertService.PublishAsync(candidate, cancellationToken);
        return Accepted(new
        {
            sent = alert is not null,
            alert
        });
    }
}

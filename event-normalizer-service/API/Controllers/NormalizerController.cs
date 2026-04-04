using F1.EventNormalizer.Service.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace F1.EventNormalizer.Service.API.Controllers;

[ApiController]
[Route("api/normalizer")]
public sealed class NormalizerController(NormalizerStatusStore statusStore) : ControllerBase
{
    [HttpGet("status")]
    public IActionResult GetStatus() => Ok(statusStore.GetSnapshot());
}

using Microsoft.AspNetCore.Mvc;

namespace GuiaNoivas.Api.Controllers;

[ApiController]
[Route("api/v1/health")]
public class HealthController : ControllerBase
{
    [HttpGet("live")]
    public IActionResult Live() => Ok(new { status = "Healthy" });

    [HttpGet("ready")]
    public IActionResult Ready() => Ok(new { status = "Ready" });
}

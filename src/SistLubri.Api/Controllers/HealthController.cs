using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SistLubri.Api.Controllers;

[ApiController]
[Route("api/health")]
[AllowAnonymous]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<HealthResponse>(StatusCodes.Status200OK)]
    public ActionResult<HealthResponse> Get()
    {
        return Ok(new HealthResponse("healthy", DateTimeOffset.UtcNow));
    }
}

public sealed record HealthResponse(string Status, DateTimeOffset Timestamp);

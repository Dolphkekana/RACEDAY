using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Contracts;

namespace RaceDay.Api.Controllers;

/// <summary>
/// Provides a lightweight endpoint for checking whether the API has started.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status200OK)]
    public ActionResult<HealthResponse> GetHealth()
    {
        return Ok(new HealthResponse(
            "Healthy",
            "RaceDay API",
            DateTimeOffset.UtcNow));
    }
}

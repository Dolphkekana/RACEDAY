using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Contracts;
using Swashbuckle.AspNetCore.Annotations;

namespace RaceDay.Api.Controllers;

// Provides a lightweight endpoint for checking whether the API has started.
[ApiController]
[Produces("application/json")]
[Route("api/[controller]")]
public sealed class HealthController : ControllerBase
{
    [SwaggerOperation(Summary = "Check the API is running", Description = "Public. Returns a simple status message.")]
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

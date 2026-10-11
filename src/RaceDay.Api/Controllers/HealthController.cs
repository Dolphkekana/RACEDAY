using Microsoft.AspNetCore.Mvc;

namespace RaceDay.Api.Controllers;

/*
 Provides health verification endpoints for monitoring API startup and operational status
*/
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    /*
     GET /api/health
     Returns the current health status of the API.
    */
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetHealth()
    {
        return Ok(new
        {
            status = "Healthy",
            service = "RaceDay API",
            timestamp = DateTime.UtcNow
        });
    }
}

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Contracts;
using RaceDay.Api.Data;
using Swashbuckle.AspNetCore.Annotations;

namespace RaceDay.Api.Controllers;

[ApiController]
[Produces("application/json")]
[Route("api/organisers/dashboard")]
[Authorize(Roles = "Organiser")]
public sealed class OrganiserDashboardController(RaceDayDbContext dbContext) : ControllerBase
{
    [SwaggerOperation(Summary = "Organiser dashboard summary", Description = "Organiser only. Returns the events created by the logged in Organiser, with enrolment totals and how many of those events are still upcoming.")]
    [HttpGet]
    [ProducesResponseType(typeof(OrganiserDashboardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<OrganiserDashboardResponse>> Get(CancellationToken cancellationToken)
    {
        var organiserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var today = DateTime.UtcNow.Date;

        var events = await dbContext.Events
            .AsNoTracking()
            .Where(raceEvent => raceEvent.OrganiserId == organiserId)
            .OrderBy(raceEvent => raceEvent.EventDate)
            .ThenBy(raceEvent => raceEvent.EventId)
            .Select(raceEvent => new OrganiserEventSummary(
                raceEvent.EventId,
                raceEvent.Name,
                raceEvent.EventDate,
                raceEvent.Location,
                raceEvent.DistanceKm,
                raceEvent.EventType!.Name,
                raceEvent.Enrolments.Count,
                raceEvent.BannerImageUrl))
            .ToListAsync(cancellationToken);

        return Ok(new OrganiserDashboardResponse(
            events.Count,
            events.Sum(raceEvent => raceEvent.EnrolmentCount),
            events.Count(raceEvent => raceEvent.EventDate.Date >= today),
            events));
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Contracts;
using RaceDay.Api.Data;
using Swashbuckle.AspNetCore.Annotations;

namespace RaceDay.Api.Controllers;

[ApiController]
[Produces("application/json")]
[Route("api/event-types")]
[AllowAnonymous]
public sealed class EventTypesController(RaceDayDbContext dbContext) : ControllerBase
{
    [SwaggerOperation(Summary = "List event types", Description = "Public. Lists the event types (Run, Walk, Cycle).")]
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<EventTypeResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<EventTypeResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var eventTypes = await dbContext.EventTypes
            .AsNoTracking()
            .OrderBy(eventType => eventType.Name)
            .Select(eventType => new EventTypeResponse(eventType.EventTypeId, eventType.Name))
            .ToListAsync(cancellationToken);

        return Ok(eventTypes);
    }

    [SwaggerOperation(Summary = "Get one event type", Description = "Public. Returns one event type by id.")]
    [HttpGet("{eventTypeId:int}")]
    [ProducesResponseType(typeof(EventTypeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventTypeResponse>> GetById(
        int eventTypeId,
        CancellationToken cancellationToken)
    {
        var eventType = await dbContext.EventTypes
            .AsNoTracking()
            .Where(candidate => candidate.EventTypeId == eventTypeId)
            .Select(candidate => new EventTypeResponse(candidate.EventTypeId, candidate.Name))
            .SingleOrDefaultAsync(cancellationToken);

        return eventType is null ? NotFound() : Ok(eventType);
    }
}

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Contracts;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using Swashbuckle.AspNetCore.Annotations;

namespace RaceDay.Api.Controllers;

[ApiController]
[Produces("application/json")]
[Route("api/events")]
public sealed class EventsController(RaceDayDbContext dbContext) : ControllerBase
{
    [SwaggerOperation(Summary = "List events", Description = "Public. Lists all events. Optional filters: eventTypeId, location, eventDate, minDistance and maxDistance.")]
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyCollection<EventResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<EventResponse>>> GetAll(
        [FromQuery] int? eventTypeId,
        [FromQuery] string? location,
        [FromQuery] DateTime? eventDate,
        [FromQuery] decimal? minDistance,
        [FromQuery] decimal? maxDistance,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Events
            .AsNoTracking()
            .Include(raceEvent => raceEvent.EventType)
            .Include(raceEvent => raceEvent.Categories)
            .AsQueryable();

        if (eventTypeId.HasValue)
        {
            query = query.Where(raceEvent => raceEvent.EventTypeId == eventTypeId.Value);
        }

        if (!string.IsNullOrWhiteSpace(location))
        {
            query = query.Where(raceEvent => raceEvent.Location.Contains(location.Trim()));
        }

        if (eventDate.HasValue)
        {
            query = query.Where(raceEvent => raceEvent.EventDate.Date == eventDate.Value.Date);
        }

        if (minDistance.HasValue)
        {
            query = query.Where(raceEvent => raceEvent.DistanceKm >= minDistance.Value);
        }

        if (maxDistance.HasValue)
        {
            query = query.Where(raceEvent => raceEvent.DistanceKm <= maxDistance.Value);
        }

        var events = await query
            .OrderBy(raceEvent => raceEvent.EventDate)
            .ThenBy(raceEvent => raceEvent.EventId)
            .Select(ToResponseExpression())
            .ToListAsync(cancellationToken);

        return Ok(events);
    }

    [SwaggerOperation(Summary = "List my events", Description = "Organiser only. Lists the events created by the logged in Organiser.")]
    [HttpGet("mine")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(IReadOnlyCollection<EventResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyCollection<EventResponse>>> GetMine(
        CancellationToken cancellationToken)
    {
        // Only return events that belong to the Organiser in the session
        var organiserId = GetUserId();
        var events = await dbContext.Events
            .AsNoTracking()
            .Where(raceEvent => raceEvent.OrganiserId == organiserId)
            .OrderBy(raceEvent => raceEvent.EventDate)
            .ThenBy(raceEvent => raceEvent.EventId)
            .Select(ToResponseExpression())
            .ToListAsync(cancellationToken);

        return Ok(events);
    }

    [SwaggerOperation(Summary = "Get one event", Description = "Public. Returns one event with its event type and categories.")]
    [HttpGet("{eventId:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventResponse>> GetById(
        int eventId,
        CancellationToken cancellationToken)
    {
        var raceEvent = await dbContext.Events
            .AsNoTracking()
            .Include(candidate => candidate.EventType)
            .Include(candidate => candidate.Categories)
            .Where(candidate => candidate.EventId == eventId)
            .Select(ToResponseExpression())
            .SingleOrDefaultAsync(cancellationToken);

        return raceEvent is null ? NotFound() : Ok(raceEvent);
    }

    [SwaggerOperation(Summary = "Create an event", Description = "Organiser only. Send JSON with eventTypeId, name, description, eventDate (today or later), location, distanceKm and an optional bannerImageUrl.")]
    [Consumes("application/json")]
    [HttpPost]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventResponse>> Create(
        EventRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateEventRequest(request);
        if (validationError is not null)
        {
            return BadRequest(new ProblemDetails { Title = validationError });
        }

        if (!await dbContext.EventTypes.AnyAsync(
                eventType => eventType.EventTypeId == request.EventTypeId,
                cancellationToken))
        {
            return NotFound(new ProblemDetails { Title = "Event type not found." });
        }

        var raceEvent = new Event
        {
            OrganiserId = GetUserId(),
            EventTypeId = request.EventTypeId,
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            EventDate = request.EventDate.Date,
            Location = request.Location.Trim(),
            DistanceKm = request.DistanceKm,
            BannerImageUrl = string.IsNullOrWhiteSpace(request.BannerImageUrl)
                ? null
                : request.BannerImageUrl.Trim()
        };

        dbContext.Events.Add(raceEvent);
        await dbContext.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetById), new { eventId = raceEvent.EventId },
            await GetResponseAsync(raceEvent.EventId, cancellationToken));
    }

    [SwaggerOperation(Summary = "Update an event", Description = "Organiser only, and only the Organiser who created the event. Uses the same JSON body as create.")]
    [Consumes("application/json")]
    [HttpPut("{eventId:int}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventResponse>> Update(
        int eventId,
        EventRequest request,
        CancellationToken cancellationToken)
    {
        var raceEvent = await dbContext.Events
            .SingleOrDefaultAsync(candidate => candidate.EventId == eventId, cancellationToken);

        if (raceEvent is null)
        {
            return NotFound();
        }

        if (raceEvent.OrganiserId != GetUserId())
        {
            return Forbid();
        }

        var validationError = ValidateEventRequest(request);
        if (validationError is not null)
        {
            return BadRequest(new ProblemDetails { Title = validationError });
        }

        if (!await dbContext.EventTypes.AnyAsync(
                eventType => eventType.EventTypeId == request.EventTypeId,
                cancellationToken))
        {
            return NotFound(new ProblemDetails { Title = "Event type not found." });
        }

        raceEvent.EventTypeId = request.EventTypeId;
        raceEvent.Name = request.Name.Trim();
        raceEvent.Description = request.Description.Trim();
        raceEvent.EventDate = request.EventDate.Date;
        raceEvent.Location = request.Location.Trim();
        raceEvent.DistanceKm = request.DistanceKm;
        raceEvent.BannerImageUrl = string.IsNullOrWhiteSpace(request.BannerImageUrl)
            ? null
            : request.BannerImageUrl.Trim();
        raceEvent.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(await GetResponseAsync(eventId, cancellationToken));
    }

    [SwaggerOperation(Summary = "Delete an event", Description = "Organiser only, and only the owner. Returns 409 if the event still has categories or enrolments.")]
    [HttpDelete("{eventId:int}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int eventId, CancellationToken cancellationToken)
    {
        var raceEvent = await dbContext.Events
            .SingleOrDefaultAsync(candidate => candidate.EventId == eventId, cancellationToken);

        if (raceEvent is null)
        {
            return NotFound();
        }

        if (raceEvent.OrganiserId != GetUserId())
        {
            return Forbid();
        }

        if (await dbContext.Enrolments.AnyAsync(
                enrolment => enrolment.EventId == eventId,
                cancellationToken))
        {
            return Conflict(new ProblemDetails
            {
                Title = "Event cannot be deleted.",
                Detail = "Existing enrolments must be removed before deleting this event."
            });
        }

        if (await dbContext.Categories.AnyAsync(
                category => category.EventId == eventId,
                cancellationToken))
        {
            return Conflict(new ProblemDetails
            {
                Title = "Event cannot be deleted.",
                Detail = "Existing categories must be removed before deleting this event."
            });
        }

        dbContext.Events.Remove(raceEvent);
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private int GetUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task<EventResponse?> GetResponseAsync(
        int eventId,
        CancellationToken cancellationToken) =>
        await dbContext.Events
            .AsNoTracking()
            .Include(raceEvent => raceEvent.EventType)
            .Include(raceEvent => raceEvent.Categories)
            .Where(raceEvent => raceEvent.EventId == eventId)
            .Select(ToResponseExpression())
            .SingleOrDefaultAsync(cancellationToken);

    private static string? ValidateEventRequest(EventRequest request)
    {
        if (request.EventDate.Date < DateTime.UtcNow.Date)
        {
            return "Event date must be today or a future date.";
        }

        if (request.DistanceKm <= 0)
        {
            return "Distance must be greater than zero.";
        }

        return null;
    }

    private static System.Linq.Expressions.Expression<Func<Event, EventResponse>> ToResponseExpression() =>
        raceEvent => new EventResponse(
            raceEvent.EventId,
            raceEvent.OrganiserId,
            raceEvent.EventTypeId,
            raceEvent.EventType!.Name,
            raceEvent.Name,
            raceEvent.Description,
            raceEvent.EventDate,
            raceEvent.Location,
            raceEvent.DistanceKm,
            raceEvent.BannerImageUrl,
            raceEvent.CreatedAt,
            raceEvent.UpdatedAt,
            raceEvent.Categories
                .OrderBy(category => category.CategoryId)
                .Select(category => new CategoryResponse(
                    category.CategoryId,
                    category.EventId,
                    category.Name,
                    category.MinimumAge,
                    category.MaximumAge,
                    category.DistanceKm))
                .ToList());
}

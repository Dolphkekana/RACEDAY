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
public sealed class EnrolmentsController(RaceDayDbContext dbContext) : ControllerBase
{
    private static readonly string[] SupportedStatuses =
    [
        "Pending",
        "Confirmed",
        "Cancelled"
    ];

    [SwaggerOperation(Summary = "Enter an event", Description = "Participant only. Send JSON with categoryId. The participant comes from the session, so one user cannot enrol another.")]
    [Consumes("application/json")]
    [HttpPost("api/events/{eventId:int}/enrolments")]
    [Authorize(Roles = "Participant")]
    [ProducesResponseType(typeof(EnrolmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EnrolmentResponse>> Create(
        int eventId,
        CreateEnrolmentRequest request,
        CancellationToken cancellationToken)
    {
        var raceEvent = await dbContext.Events
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.EventId == eventId, cancellationToken);
        if (raceEvent is null)
        {
            return NotFound(new ProblemDetails { Title = "Event not found." });
        }

        var category = await dbContext.Categories
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.CategoryId == request.CategoryId,
                cancellationToken);
        if (category is null)
        {
            return NotFound(new ProblemDetails { Title = "Category not found." });
        }

        if (category.EventId != eventId)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Category does not belong to the event."
            });
        }

        var participantId = GetUserId();
        if (await dbContext.Enrolments.AnyAsync(
                enrolment => enrolment.ParticipantId == participantId
                    && enrolment.EventId == eventId,
                cancellationToken))
        {
            return Conflict(new ProblemDetails
            {
                Title = "Participant is already enrolled in this event."
            });
        }

        var enrolment = new Enrolment
        {
            ParticipantId = participantId,
            EventId = eventId,
            CategoryId = request.CategoryId,
            Status = "Pending"
        };

        dbContext.Enrolments.Add(enrolment);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Participant is already enrolled in this event."
            });
        }

        return CreatedAtAction(
            nameof(GetById),
            new { enrolmentId = enrolment.EnrolmentId },
            await GetResponseAsync(enrolment.EnrolmentId, cancellationToken));
    }

    [SwaggerOperation(Summary = "List my enrolments", Description = "Participant only. Lists the enrolments of the logged in Participant.")]
    [HttpGet("api/enrolments/my")]
    [Authorize(Roles = "Participant")]
    [ProducesResponseType(typeof(IReadOnlyCollection<EnrolmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyCollection<EnrolmentResponse>>> GetMine(
        CancellationToken cancellationToken)
    {
        var enrolments = await dbContext.Enrolments
            .AsNoTracking()
            .Where(enrolment => enrolment.ParticipantId == GetUserId())
            .OrderByDescending(enrolment => enrolment.EnrolmentDate)
            .Select(ToResponseExpression())
            .ToListAsync(cancellationToken);

        return Ok(enrolments);
    }

    [SwaggerOperation(Summary = "Get one enrolment", Description = "The Participant who owns the enrolment or the Organiser of the event.")]
    [HttpGet("api/enrolments/{enrolmentId:int}")]
    [Authorize]
    [ProducesResponseType(typeof(EnrolmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EnrolmentResponse>> GetById(
        int enrolmentId,
        CancellationToken cancellationToken)
    {
        var enrolment = await dbContext.Enrolments
            .AsNoTracking()
            .Where(candidate => candidate.EnrolmentId == enrolmentId)
            .Select(ToResponseExpression())
            .SingleOrDefaultAsync(cancellationToken);
        if (enrolment is null)
        {
            return NotFound();
        }

        if (!await CanManageOrOwnAsync(enrolment, cancellationToken))
        {
            return Forbid();
        }

        return Ok(enrolment);
    }

    [SwaggerOperation(Summary = "List enrolments for an event", Description = "Organiser only, for an event they own.")]
    [HttpGet("api/events/{eventId:int}/enrolments")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(IReadOnlyCollection<EnrolmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyCollection<EnrolmentResponse>>> GetForEvent(
        int eventId,
        CancellationToken cancellationToken)
    {
        var raceEvent = await dbContext.Events
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.EventId == eventId, cancellationToken);
        if (raceEvent is null)
        {
            return NotFound();
        }

        if (raceEvent.OrganiserId != GetUserId())
        {
            return Forbid();
        }

        var enrolments = await dbContext.Enrolments
            .AsNoTracking()
            .Where(enrolment => enrolment.EventId == eventId)
            .OrderBy(enrolment => enrolment.EnrolmentId)
            .Select(ToResponseExpression())
            .ToListAsync(cancellationToken);

        return Ok(enrolments);
    }

    [SwaggerOperation(Summary = "Change an enrolment status", Description = "Organiser only, for an event they own. Send JSON with status: Pending, Confirmed or Cancelled.")]
    [Consumes("application/json")]
    [HttpPut("api/enrolments/{enrolmentId:int}/status")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(EnrolmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EnrolmentResponse>> UpdateStatus(
        int enrolmentId,
        UpdateEnrolmentStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (!SupportedStatuses.Contains(request.Status, StringComparer.Ordinal))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Unsupported enrolment status.",
                Detail = "Status must be Pending, Confirmed, or Cancelled."
            });
        }

        var enrolment = await dbContext.Enrolments
            .Include(candidate => candidate.Event)
            .SingleOrDefaultAsync(candidate => candidate.EnrolmentId == enrolmentId, cancellationToken);
        if (enrolment is null)
        {
            return NotFound();
        }

        if (enrolment.Event!.OrganiserId != GetUserId())
        {
            return Forbid();
        }

        enrolment.Status = request.Status;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(await GetResponseAsync(enrolmentId, cancellationToken));
    }

    private int GetUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task<bool> CanManageOrOwnAsync(
        EnrolmentResponse enrolment,
        CancellationToken cancellationToken)
    {
        if (enrolment.ParticipantId == GetUserId())
        {
            return true;
        }

        return User.IsInRole("Organiser")
            && await dbContext.Events.AnyAsync(
                raceEvent => raceEvent.EventId == enrolment.EventId
                    && raceEvent.OrganiserId == GetUserId(),
                cancellationToken);
    }

    private async Task<EnrolmentResponse?> GetResponseAsync(
        int enrolmentId,
        CancellationToken cancellationToken) =>
        await dbContext.Enrolments
            .AsNoTracking()
            .Where(enrolment => enrolment.EnrolmentId == enrolmentId)
            .Select(ToResponseExpression())
            .SingleOrDefaultAsync(cancellationToken);

    private static System.Linq.Expressions.Expression<Func<Enrolment, EnrolmentResponse>>
        ToResponseExpression() =>
        enrolment => new EnrolmentResponse(
            enrolment.EnrolmentId,
            enrolment.ParticipantId,
            enrolment.Participant!.FirstName + " " + enrolment.Participant.LastName,
            enrolment.EventId,
            enrolment.Event!.Name,
            enrolment.CategoryId,
            enrolment.Category!.Name,
            enrolment.EnrolmentDate,
            enrolment.Status);
}

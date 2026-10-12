using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Contracts;
using RaceDay.Api.Data;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
public sealed class ResultsController(RaceDayDbContext dbContext) : ControllerBase
{
    /// <summary>Records a finish result for an enrolment managed by the event Organiser.</summary>
    [HttpPost("api/enrolments/{enrolmentId:int}/result")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ResultResponse>> Create(
        int enrolmentId,
        ResultRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateRequest(request);
        if (validationError is not null)
        {
            return BadRequest(new ProblemDetails { Title = validationError });
        }

        var enrolment = await dbContext.Enrolments
            .Include(candidate => candidate.Event)
            .SingleOrDefaultAsync(candidate => candidate.EnrolmentId == enrolmentId, cancellationToken);
        if (enrolment is null)
        {
            return NotFound(new ProblemDetails { Title = "Enrolment not found." });
        }

        if (enrolment.Event!.OrganiserId != GetUserId())
        {
            return Forbid();
        }

        if (await dbContext.Results.AnyAsync(
                result => result.EnrolmentId == enrolmentId,
                cancellationToken))
        {
            return Conflict(new ProblemDetails
            {
                Title = "A result already exists for this enrolment."
            });
        }

        var result = new Result
        {
            EnrolmentId = enrolmentId,
            FinishTime = request.FinishTime,
            FinishingPosition = request.FinishingPosition
        };
        dbContext.Results.Add(result);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict(new ProblemDetails
            {
                Title = "A result already exists for this enrolment."
            });
        }

        return CreatedAtAction(
            nameof(GetMine),
            null,
            await GetResponseAsync(result.ResultId, cancellationToken));
    }

    /// <summary>Updates a result for an event managed by the authenticated Organiser.</summary>
    [HttpPut("api/results/{resultId:int}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResultResponse>> Update(
        int resultId,
        ResultRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateRequest(request);
        if (validationError is not null)
        {
            return BadRequest(new ProblemDetails { Title = validationError });
        }

        var result = await dbContext.Results
            .Include(candidate => candidate.Enrolment!)
                .ThenInclude(enrolment => enrolment.Event)
            .SingleOrDefaultAsync(candidate => candidate.ResultId == resultId, cancellationToken);
        if (result is null)
        {
            return NotFound();
        }

        if (result.Enrolment!.Event!.OrganiserId != GetUserId())
        {
            return Forbid();
        }

        result.FinishTime = request.FinishTime;
        result.FinishingPosition = request.FinishingPosition;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(await GetResponseAsync(resultId, cancellationToken));
    }

    /// <summary>Returns race results belonging to the authenticated Participant.</summary>
    [HttpGet("api/results/my")]
    [Authorize(Roles = "Participant")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyCollection<ResultResponse>>> GetMine(
        CancellationToken cancellationToken)
    {
        var results = await dbContext.Results
            .AsNoTracking()
            .Where(result => result.Enrolment!.ParticipantId == GetUserId())
            .OrderByDescending(result => result.Enrolment!.Event!.EventDate)
            .ThenBy(result => result.ResultId)
            .Select(ToResponseExpression())
            .ToListAsync(cancellationToken);

        return Ok(results);
    }

    private int GetUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task<ResultResponse?> GetResponseAsync(
        int resultId,
        CancellationToken cancellationToken) =>
        await dbContext.Results
            .AsNoTracking()
            .Where(result => result.ResultId == resultId)
            .Select(ToResponseExpression())
            .SingleOrDefaultAsync(cancellationToken);

    private static string? ValidateRequest(ResultRequest request)
    {
        if (request.FinishTime <= TimeSpan.Zero)
        {
            return "Finish time must be greater than zero.";
        }

        if (request.FinishTime >= TimeSpan.FromDays(1))
        {
            return "Finish time must be less than 24 hours.";
        }

        if (request.FinishingPosition <= 0)
        {
            return "Finishing position must be greater than zero.";
        }

        return null;
    }

    private static System.Linq.Expressions.Expression<Func<Result, ResultResponse>>
        ToResponseExpression() =>
        result => new ResultResponse(
            result.ResultId,
            result.EnrolmentId,
            result.Enrolment!.ParticipantId,
            result.Enrolment.Participant!.FirstName + " " + result.Enrolment.Participant.LastName,
            result.Enrolment.EventId,
            result.Enrolment.Event!.Name,
            result.Enrolment.Event.EventDate,
            result.Enrolment.CategoryId,
            result.Enrolment.Category!.Name,
            result.FinishTime,
            result.FinishingPosition,
            result.RecordedAt);
}

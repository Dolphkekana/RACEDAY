using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Contracts;
using RaceDay.Api.Data;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
public sealed class CategoriesController(RaceDayDbContext dbContext) : ControllerBase
{
    /// <summary>Lists categories belonging to an event.</summary>
    [HttpGet("api/events/{eventId:int}/categories")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyCollection<CategoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyCollection<CategoryResponse>>> GetForEvent(
        int eventId,
        CancellationToken cancellationToken)
    {
        if (!await dbContext.Events.AnyAsync(raceEvent => raceEvent.EventId == eventId, cancellationToken))
        {
            return NotFound();
        }

        var categories = await dbContext.Categories
            .AsNoTracking()
            .Where(category => category.EventId == eventId)
            .OrderBy(category => category.CategoryId)
            .Select(ToResponseExpression())
            .ToListAsync(cancellationToken);

        return Ok(categories);
    }

    [HttpGet("api/categories/{categoryId:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryResponse>> GetById(
        int categoryId,
        CancellationToken cancellationToken)
    {
        var category = await dbContext.Categories
            .AsNoTracking()
            .Where(candidate => candidate.CategoryId == categoryId)
            .Select(ToResponseExpression())
            .SingleOrDefaultAsync(cancellationToken);

        return category is null ? NotFound() : Ok(category);
    }

    /// <summary>Creates a category for an event owned by the authenticated Organiser.</summary>
    [HttpPost("api/events/{eventId:int}/categories")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryResponse>> Create(
        int eventId,
        CategoryRequest request,
        CancellationToken cancellationToken)
    {
        var raceEvent = await dbContext.Events
            .SingleOrDefaultAsync(candidate => candidate.EventId == eventId, cancellationToken);
        if (raceEvent is null)
        {
            return NotFound(new ProblemDetails { Title = "Event not found." });
        }

        if (raceEvent.OrganiserId != GetUserId())
        {
            return Forbid();
        }

        var validationError = ValidateCategoryRequest(request);
        if (validationError is not null)
        {
            return BadRequest(new ProblemDetails { Title = validationError });
        }

        if (await dbContext.Categories.AnyAsync(
                category => category.EventId == eventId && category.Name == request.Name.Trim(),
                cancellationToken))
        {
            return Conflict(new ProblemDetails { Title = "A category with this name already exists for the event." });
        }

        var category = new Category
        {
            EventId = eventId,
            Name = request.Name.Trim(),
            MinimumAge = request.MinimumAge,
            MaximumAge = request.MaximumAge,
            DistanceKm = request.DistanceKm
        };
        dbContext.Categories.Add(category);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { categoryId = category.CategoryId },
            await GetResponseAsync(category.CategoryId, cancellationToken));
    }

    [HttpPut("api/categories/{categoryId:int}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryResponse>> Update(
        int categoryId,
        CategoryRequest request,
        CancellationToken cancellationToken)
    {
        var category = await dbContext.Categories
            .Include(candidate => candidate.Event)
            .SingleOrDefaultAsync(candidate => candidate.CategoryId == categoryId, cancellationToken);
        if (category is null)
        {
            return NotFound();
        }

        if (category.Event!.OrganiserId != GetUserId())
        {
            return Forbid();
        }

        var validationError = ValidateCategoryRequest(request);
        if (validationError is not null)
        {
            return BadRequest(new ProblemDetails { Title = validationError });
        }

        if (await dbContext.Categories.AnyAsync(
                candidate => candidate.CategoryId != categoryId
                    && candidate.EventId == category.EventId
                    && candidate.Name == request.Name.Trim(),
                cancellationToken))
        {
            return Conflict(new ProblemDetails { Title = "A category with this name already exists for the event." });
        }

        category.Name = request.Name.Trim();
        category.MinimumAge = request.MinimumAge;
        category.MaximumAge = request.MaximumAge;
        category.DistanceKm = request.DistanceKm;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(await GetResponseAsync(categoryId, cancellationToken));
    }

    /// <summary>Deletes a category when no enrolment uses it.</summary>
    [HttpDelete("api/categories/{categoryId:int}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int categoryId, CancellationToken cancellationToken)
    {
        var category = await dbContext.Categories
            .Include(candidate => candidate.Event)
            .SingleOrDefaultAsync(candidate => candidate.CategoryId == categoryId, cancellationToken);
        if (category is null)
        {
            return NotFound();
        }

        if (category.Event!.OrganiserId != GetUserId())
        {
            return Forbid();
        }

        if (await dbContext.Enrolments.AnyAsync(
                enrolment => enrolment.CategoryId == categoryId,
                cancellationToken))
        {
            return Conflict(new ProblemDetails
            {
                Title = "Category cannot be deleted.",
                Detail = "Existing enrolments use this category."
            });
        }

        dbContext.Categories.Remove(category);
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private int GetUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task<CategoryResponse?> GetResponseAsync(
        int categoryId,
        CancellationToken cancellationToken) =>
        await dbContext.Categories
            .AsNoTracking()
            .Where(category => category.CategoryId == categoryId)
            .Select(ToResponseExpression())
            .SingleOrDefaultAsync(cancellationToken);

    private static string? ValidateCategoryRequest(CategoryRequest request)
    {
        if (request.MinimumAge.HasValue
            && request.MaximumAge.HasValue
            && request.MinimumAge > request.MaximumAge)
        {
            return "Minimum age must not exceed maximum age.";
        }

        if (request.DistanceKm.HasValue && request.DistanceKm <= 0)
        {
            return "Distance must be greater than zero.";
        }

        return null;
    }

    private static System.Linq.Expressions.Expression<Func<Category, CategoryResponse>> ToResponseExpression() =>
        category => new CategoryResponse(
            category.CategoryId,
            category.EventId,
            category.Name,
            category.MinimumAge,
            category.MaximumAge,
            category.DistanceKm);
}

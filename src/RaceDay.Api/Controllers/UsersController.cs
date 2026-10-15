using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Contracts;
using RaceDay.Api.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace RaceDay.Api.Controllers;

[ApiController]
[Produces("application/json")]
[Route("api/users")]
[Authorize]
public sealed class UsersController(IAuthenticationService authenticationService) : ControllerBase
{
    [SwaggerOperation(Summary = "View my profile", Description = "Any logged in user. Returns the profile of the user in the current session.")]
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserResponse>> GetCurrentProfile(
        CancellationToken cancellationToken)
    {
        var userId = GetAuthenticatedUserId();
        var response = await authenticationService.GetProfileAsync(userId, cancellationToken);
        return response is null ? Unauthorized() : Ok(response);
    }

    [SwaggerOperation(Summary = "Update my profile", Description = "Any logged in user. Updates firstName, lastName, phoneNumber and dateOfBirth. Email and role cannot be changed here.")]
    [Consumes("application/json")]
    [HttpPut("me")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserResponse>> UpdateCurrentProfile(
        UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetAuthenticatedUserId();
        var response = await authenticationService.UpdateProfileAsync(
            userId,
            request,
            cancellationToken);
        return response is null ? Unauthorized() : Ok(response);
    }

    private int GetAuthenticatedUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}

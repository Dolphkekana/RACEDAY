using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Auth;
using RaceDay.Api.Contracts;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    IAuthenticationService authenticationService,
    IHttpContextAccessor httpContextAccessor) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponse>> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(request.Role, "Participant", StringComparison.Ordinal))
        {
            return Forbid();
        }

        try
        {
            var response = await authenticationService.RegisterParticipantAsync(request, cancellationToken);
            return Created("/api/users/me", response);
        }
        catch (DuplicateEmailException)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Email already registered.",
                Detail = "An account with this email address already exists.",
                Status = StatusCodes.Status409Conflict
            });
        }
    }

    [HttpPost("provision-organiser")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponse>> ProvisionOrganiser(
        ProvisionOrganiserRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await authenticationService.ProvisionOrganiserAsync(
                request,
                cancellationToken);
            return Created("/api/users/me", response);
        }
        catch (DuplicateEmailException)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Email already registered.",
                Detail = "An account with this email address already exists.",
                Status = StatusCodes.Status409Conflict
            });
        }
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var response = await authenticationService.ValidateCredentialsAsync(
            request,
            cancellationToken);

        if (response is null)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Invalid credentials.",
                Detail = "The email or password is incorrect.",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        var session = httpContextAccessor.HttpContext!.Session;
        session.SetInt32(UserSession.UserIdKey, response.UserId);
        session.SetString(UserSession.RoleKey, response.Role);
        return Ok(response);
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Logout()
    {
        httpContextAccessor.HttpContext!.Session.Clear();
        return NoContent();
    }
}

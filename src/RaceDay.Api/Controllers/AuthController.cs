using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Auth;
using RaceDay.Api.Contracts;
using RaceDay.Api.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace RaceDay.Api.Controllers;

[ApiController]
[Produces("application/json")]
[Route("api/auth")]
public sealed class AuthController(
    IAuthenticationService authenticationService,
    IHttpContextAccessor httpContextAccessor) : ControllerBase
{
    [SwaggerOperation(Summary = "Register a new account", Description = "Creates an Organiser or Participant account. Send JSON with firstName, lastName, email, password (8+ chars with upper, lower and a digit), phoneNumber, dateOfBirth (yyyy-MM-dd) and role. The password is stored hashed.")]
    [Consumes("application/json")]
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponse>> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await authenticationService.RegisterAsync(request, cancellationToken);
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

    [SwaggerOperation(Summary = "Create another Organiser account", Description = "Organiser only. Creates a new Organiser account using the same fields as registration without the role.")]
    [Consumes("application/json")]
    [HttpPost("provision-organiser")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
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

    [SwaggerOperation(Summary = "Log in and start a session", Description = "Checks the email and password. On success the user id and role are stored in the server-side session and the RaceDay.Session cookie is returned.")]
    [Consumes("application/json")]
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

    [SwaggerOperation(Summary = "Log out and clear the session", Description = "Any logged in user. Clears the server-side session.")]
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

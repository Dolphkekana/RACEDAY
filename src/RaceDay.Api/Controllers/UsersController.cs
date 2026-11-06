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
public sealed class UsersController(
    IAuthenticationService authenticationService,
    IImageStorage imageStorage) : ControllerBase
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

    [SwaggerOperation(Summary = "Upload my profile picture", Description = "Any logged in user. Send a multipart file named file (JPG, PNG, WEBP, or GIF, up to 5 MB). The image is stored and the URL is saved on the profile.")]
    [Consumes("multipart/form-data")]
    [HttpPost("me/picture")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserResponse>> UploadProfilePicture(
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        // The picture is stored first. Only the returned URL is written onto the user row.
        var validationError = ImageUploadRules.Validate(file);
        if (validationError is not null || file is null)
        {
            return BadRequest(new ProblemDetails { Title = validationError ?? "Choose an image to upload." });
        }

        await using var stream = file.OpenReadStream();
        var fileName = ImageUploadRules.CreateFileName(file.ContentType);
        var imageUrl = await imageStorage.UploadAsync(
            "profiles",
            fileName,
            stream,
            file.ContentType,
            cancellationToken);

        var response = await authenticationService.SetProfileImageAsync(
            GetAuthenticatedUserId(),
            imageUrl,
            cancellationToken);
        return response is null ? Unauthorized() : Ok(response);
    }

    private int GetAuthenticatedUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}

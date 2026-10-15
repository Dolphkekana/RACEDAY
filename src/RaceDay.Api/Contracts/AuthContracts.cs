using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Contracts;

/// <summary>
/// Payload for creating a new Organiser or Participant account.
/// </summary>
public sealed record RegisterRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public required string FirstName { get; init; }

    [Required, StringLength(100, MinimumLength = 2)]
    public required string LastName { get; init; }

    [Required, EmailAddress, StringLength(255)]
    public required string Email { get; init; }

    [Required, StringLength(64, MinimumLength = 8)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$")]
    public required string Password { get; init; }

    [Required, StringLength(20, MinimumLength = 7)]
    public required string PhoneNumber { get; init; }

    [Required]
    public DateTime DateOfBirth { get; init; }

    // The user selects either Organiser or Participant when registering
    [Required, RegularExpression("^(Organiser|Participant)$", ErrorMessage = "Role must be Organiser or Participant.")]
    public required string Role { get; init; }
}

/// <summary>
/// Payload for provisioning an Organiser account from an authenticated Organiser account.
/// </summary>
public sealed record ProvisionOrganiserRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public required string FirstName { get; init; }

    [Required, StringLength(100, MinimumLength = 2)]
    public required string LastName { get; init; }

    [Required, EmailAddress, StringLength(255)]
    public required string Email { get; init; }

    [Required, StringLength(64, MinimumLength = 8)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$")]
    public required string Password { get; init; }

    [Required, StringLength(20, MinimumLength = 7)]
    public required string PhoneNumber { get; init; }

    [Required]
    public DateTime DateOfBirth { get; init; }
}

/// <summary>
/// Payload for authenticating an existing RaceDay account.
/// </summary>
public sealed record LoginRequest
{
    [Required, EmailAddress, StringLength(255)]
    public required string Email { get; init; }

    [Required]
    public required string Password { get; init; }
}

/// <summary>
/// Safe user information returned by authentication and profile endpoints.
/// </summary>
public sealed record UserResponse(
    int UserId,
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber,
    DateTime DateOfBirth,
    string Role);

/// <summary>
/// Payload for updating the authenticated user's editable profile details.
/// </summary>
public sealed record UpdateProfileRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public required string FirstName { get; init; }

    [Required, StringLength(100, MinimumLength = 2)]
    public required string LastName { get; init; }

    [Required, StringLength(20, MinimumLength = 7)]
    public required string PhoneNumber { get; init; }

    [Required]
    public DateTime DateOfBirth { get; init; }
}

using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models;

public sealed class LoginViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public sealed class RegisterViewModel
{
    [Required, StringLength(100, MinimumLength = 2)]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 2)]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(64, MinimumLength = 8)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$",
        ErrorMessage = "Password must contain an upper case letter, a lower case letter, and a digit.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required, StringLength(20, MinimumLength = 7)]
    [Display(Name = "Phone number")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Date of birth")]
    public DateTime DateOfBirth { get; set; } = new(1995, 1, 1);

    [Required]
    [RegularExpression("^(Organiser|Participant)$", ErrorMessage = "Choose Organiser or Participant.")]
    public string Role { get; set; } = "Participant";
}

namespace RaceDay.Api.Models;

/*
 Represents a user account within the RaceDay system.
 Supports Organiser and Participant roles as per Part 1 ERD.
*/
public class User
{
    public int UserId { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public DateTime DateOfBirth { get; set; }

    public string Role { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties for EF Core relationships 
    public ICollection<Event> OrganizedEvents { get; set; } = new List<Event>();

    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}

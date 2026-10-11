namespace RaceDay.Api.Models;

// Represents a participant's registration for an event category.

public class Enrolment
{
    public int EnrolmentId { get; set; }

    public int ParticipantId { get; set; }

    public int EventId { get; set; }

    public int CategoryId { get; set; }

    public DateTime EnrolmentDate { get; set; } = DateTime.UtcNow;

    public string Status { get; set; } = "Pending";

    /* Navigation properties */
    public User? Participant { get; set; }

    public Event? Event { get; set; }

    public Category? Category { get; set; }

    public Result? Result { get; set; }
}

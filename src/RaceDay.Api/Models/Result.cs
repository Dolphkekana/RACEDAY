namespace RaceDay.Api.Models;

// Represents the finish time and finishing position for an enrolment. 

public class Result
{
    public int ResultId { get; set; }

    public int EnrolmentId { get; set; }

    public TimeSpan FinishTime { get; set; }

    public int FinishingPosition { get; set; }

    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    /* Navigation property */
    public Enrolment? Enrolment { get; set; }
}

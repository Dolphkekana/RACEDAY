namespace RaceDay.Api.Models;

// Represents an age or distance category within a specific event.

public class Category
{
    public int CategoryId { get; set; }

    public int EventId { get; set; }

    public string Name { get; set; } = string.Empty;

    public int? MinimumAge { get; set; }

    public int? MaximumAge { get; set; }

    public decimal? DistanceKm { get; set; }

    /* Navigation properties */
    public Event? Event { get; set; }

    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}

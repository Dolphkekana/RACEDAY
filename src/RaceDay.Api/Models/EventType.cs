namespace RaceDay.Api.Models;

// Represents the classification of an event (Run, Walk, Cycle).

public class EventType
{
    public int EventTypeId { get; set; }

    public string Name { get; set; } = string.Empty;

    // Navigation property for events of this type 
    public ICollection<Event> Events { get; set; } = new List<Event>();
}

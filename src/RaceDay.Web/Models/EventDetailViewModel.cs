using RaceDay.Web.Api;

namespace RaceDay.Web.Models;

public sealed class EventDetailViewModel
{
    public required EventModel Event { get; init; }

    public int CategoryId { get; set; }

    public EnrolmentModel? ExistingEnrolment { get; set; }
}

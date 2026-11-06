namespace RaceDay.Api.Contracts;

public sealed record OrganiserEventSummary(
    int EventId,
    string Name,
    DateTime EventDate,
    string Location,
    decimal DistanceKm,
    string EventType,
    int EnrolmentCount,
    string? BannerImageUrl);

public sealed record OrganiserDashboardResponse(
    int EventCount,
    int EnrolmentCount,
    int UpcomingEventCount,
    IReadOnlyCollection<OrganiserEventSummary> Events);

namespace RaceDay.Web.Api;

public sealed record UserModel(
    int UserId,
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber,
    DateTime DateOfBirth,
    string Role,
    string? ProfileImageUrl);

public sealed record EventModel(
    int EventId,
    int OrganiserId,
    int EventTypeId,
    string EventType,
    string Name,
    string Description,
    DateTime EventDate,
    string Location,
    decimal DistanceKm,
    string? BannerImageUrl,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    List<CategoryModel> Categories);

public sealed record CategoryModel(
    int CategoryId,
    int EventId,
    string Name,
    int? MinimumAge,
    int? MaximumAge,
    decimal? DistanceKm);

public sealed record EventTypeModel(int EventTypeId, string Name);

public sealed record EnrolmentModel(
    int EnrolmentId,
    int ParticipantId,
    string ParticipantName,
    int EventId,
    string EventName,
    int CategoryId,
    string CategoryName,
    DateTime EnrolmentDate,
    string Status);

public sealed record ResultModel(
    int ResultId,
    int EnrolmentId,
    int ParticipantId,
    string ParticipantName,
    int EventId,
    string EventName,
    DateTime EventDate,
    int CategoryId,
    string CategoryName,
    TimeSpan FinishTime,
    int FinishingPosition,
    DateTime RecordedAt,
    int FinisherCount);

public sealed record DashboardModel(
    int EventCount,
    int EnrolmentCount,
    int UpcomingEventCount,
    List<DashboardEventModel> Events);

public sealed record DashboardEventModel(
    int EventId,
    string Name,
    DateTime EventDate,
    string Location,
    decimal DistanceKm,
    string EventType,
    int EnrolmentCount,
    string? BannerImageUrl);

public sealed record EventWriteModel(
    int EventTypeId,
    string Name,
    string Description,
    DateTime EventDate,
    string Location,
    decimal DistanceKm,
    string? BannerImageUrl);

public sealed record CategoryWriteModel(
    string Name,
    int? MinimumAge,
    int? MaximumAge,
    decimal? DistanceKm);

public sealed record ProfileWriteModel(
    string FirstName,
    string LastName,
    string PhoneNumber,
    DateTime DateOfBirth);

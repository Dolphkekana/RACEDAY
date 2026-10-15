using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Contracts;

public sealed record ResultRequest
{
    [Required]
    public TimeSpan FinishTime { get; init; }

    [Range(1, int.MaxValue)]
    public int FinishingPosition { get; init; }
}

public sealed record ResultResponse(
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
    DateTime RecordedAt);

using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Contracts;

public sealed record CreateEnrolmentRequest
{
    [Range(1, int.MaxValue)]
    public int CategoryId { get; init; }
}

public sealed record UpdateEnrolmentStatusRequest
{
    [Required]
    public required string Status { get; init; }
}

public sealed record EnrolmentResponse(
    int EnrolmentId,
    int ParticipantId,
    string ParticipantName,
    int EventId,
    string EventName,
    int CategoryId,
    string CategoryName,
    DateTime EnrolmentDate,
    string Status);

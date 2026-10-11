namespace RaceDay.Api.Contracts;

/// <summary>
/// Describes the current availability of the RaceDay API.
/// </summary>
public sealed record HealthResponse(
    string Status,
    string Service,
    DateTimeOffset Timestamp);

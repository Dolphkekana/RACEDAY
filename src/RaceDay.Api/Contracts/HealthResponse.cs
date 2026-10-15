namespace RaceDay.Api.Contracts;

// Describes the current availability of the RaceDay API.
public sealed record HealthResponse(
    string Status,
    string Service,
    DateTimeOffset Timestamp);

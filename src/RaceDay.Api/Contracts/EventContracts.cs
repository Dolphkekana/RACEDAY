using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Contracts;

public sealed record EventRequest
{
    [Range(1, int.MaxValue)]
    public int EventTypeId { get; init; }

    [Required, StringLength(150, MinimumLength = 2)]
    public required string Name { get; init; }

    [Required, StringLength(1000, MinimumLength = 2)]
    public required string Description { get; init; }

    [Required]
    public DateTime EventDate { get; init; }

    [Required, StringLength(200, MinimumLength = 2)]
    public required string Location { get; init; }

    [Range(typeof(decimal), "0.01", "9999.99")]
    public decimal DistanceKm { get; init; }

    [StringLength(500)]
    public string? BannerImageUrl { get; init; }
}

public sealed record EventResponse(
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
    IReadOnlyCollection<CategoryResponse> Categories);

public sealed record EventTypeResponse(int EventTypeId, string Name);

public sealed record CategoryRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public required string Name { get; init; }

    [Range(0, 150)]
    public int? MinimumAge { get; init; }

    [Range(0, 150)]
    public int? MaximumAge { get; init; }

    [Range(typeof(decimal), "0.01", "9999.99")]
    public decimal? DistanceKm { get; init; }
}

public sealed record CategoryResponse(
    int CategoryId,
    int EventId,
    string Name,
    int? MinimumAge,
    int? MaximumAge,
    decimal? DistanceKm);

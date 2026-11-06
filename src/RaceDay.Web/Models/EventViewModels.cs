using System.ComponentModel.DataAnnotations;
using RaceDay.Web.Api;

namespace RaceDay.Web.Models;

public sealed class EventFilterViewModel
{
    [Display(Name = "Search")]
    public string? Name { get; set; }

    [Display(Name = "Event type")]
    public int? EventTypeId { get; set; }

    public string? Location { get; set; }

    [Display(Name = "Min km")]
    public decimal? MinDistance { get; set; }

    [Display(Name = "Max km")]
    public decimal? MaxDistance { get; set; }

    public List<EventTypeModel> EventTypes { get; set; } = [];

    public List<EventModel> Events { get; set; } = [];
}

public sealed class EventFormViewModel
{
    public int? EventId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Choose an event type.")]
    [Display(Name = "Event type")]
    public int EventTypeId { get; set; }

    [Required, StringLength(150, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(1000, MinimumLength = 2)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Event date")]
    public DateTime EventDate { get; set; } = DateTime.UtcNow.Date.AddDays(14);

    [Required, StringLength(200, MinimumLength = 2)]
    public string Location { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "9999.99")]
    [Display(Name = "Distance (km)")]
    public decimal DistanceKm { get; set; }

    public string? BannerImageUrl { get; set; }

    public IFormFile? Banner { get; set; }

    public List<EventTypeModel> EventTypes { get; set; } = [];
}

public sealed class CategoryFormViewModel
{
    public int EventId { get; set; }

    public string EventName { get; set; } = string.Empty;

    public int? CategoryId { get; set; }

    [Required, StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Range(0, 150)]
    [Display(Name = "Minimum age")]
    public int? MinimumAge { get; set; }

    [Range(0, 150)]
    [Display(Name = "Maximum age")]
    public int? MaximumAge { get; set; }

    [Display(Name = "Distance (km)")]
    public decimal? DistanceKm { get; set; }

    public List<CategoryModel> Categories { get; set; } = [];
}

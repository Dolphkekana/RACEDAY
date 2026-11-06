using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Api;
using RaceDay.Web.Models;

namespace RaceDay.Web.Controllers;

public sealed class HomeController(RaceDayApiClient apiClient) : Controller
{
    public async Task<IActionResult> Index(
        string? name,
        int? eventTypeId,
        string? location,
        decimal? minDistance,
        decimal? maxDistance,
        CancellationToken cancellationToken)
    {
        var model = new EventFilterViewModel
        {
            Name = name,
            EventTypeId = eventTypeId,
            Location = location,
            MinDistance = minDistance,
            MaxDistance = maxDistance
        };

        try
        {
            model.EventTypes = await apiClient.GetEventTypesAsync(cancellationToken);
            model.Events = await apiClient.GetEventsAsync(
                name,
                eventTypeId,
                location,
                upcoming: true,
                minDistance,
                maxDistance,
                cancellationToken);
        }
        catch (ApiException exception)
        {
            TempData["Error"] = exception.Message;
        }

        return View(model);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View();
}

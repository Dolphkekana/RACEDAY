using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Api;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;

namespace RaceDay.Web.Controllers;

[RequireLogin(Role = "Organiser")]
public sealed class ManageEventsController(RaceDayApiClient apiClient) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        try
        {
            var events = await apiClient.GetMyEventsAsync(cancellationToken);
            return View(events);
        }
        catch (ApiException exception)
        {
            TempData["Error"] = exception.Message;
            return View(new List<EventModel>());
        }
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        return View("Form", await EmptyFormAsync(cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EventFormViewModel model, CancellationToken cancellationToken)
    {
        model.EventTypes = await LoadTypesAsync(cancellationToken);
        if (!ModelState.IsValid)
        {
            return View("Form", model);
        }

        try
        {
            var created = await apiClient.CreateEventAsync(ToWrite(model), cancellationToken);
            if (model.Banner is { Length: > 0 })
            {
                await apiClient.UploadBannerAsync(created.EventId, model.Banner, cancellationToken);
            }

            TempData["Success"] = "Event created.";
            return RedirectToAction(nameof(Edit), new { id = created.EventId });
        }
        catch (ApiException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return View("Form", model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        try
        {
            var raceEvent = await apiClient.GetEventAsync(id, cancellationToken);
            var model = ToForm(raceEvent);
            model.EventTypes = await LoadTypesAsync(cancellationToken);
            return View("Form", model);
        }
        catch (ApiException exception)
        {
            TempData["Error"] = exception.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EventFormViewModel model, CancellationToken cancellationToken)
    {
        model.EventId = id;
        model.EventTypes = await LoadTypesAsync(cancellationToken);
        if (!ModelState.IsValid)
        {
            return View("Form", model);
        }

        try
        {
            await apiClient.UpdateEventAsync(id, ToWrite(model), cancellationToken);
            if (model.Banner is { Length: > 0 })
            {
                await apiClient.UploadBannerAsync(id, model.Banner, cancellationToken);
            }

            TempData["Success"] = "Event saved.";
            return RedirectToAction(nameof(Edit), new { id });
        }
        catch (ApiException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return View("Form", model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            await apiClient.DeleteEventAsync(id, cancellationToken);
            TempData["Success"] = "Event deleted.";
        }
        catch (ApiException exception)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<EventFormViewModel> EmptyFormAsync(CancellationToken cancellationToken)
    {
        return new EventFormViewModel
        {
            EventTypes = await LoadTypesAsync(cancellationToken)
        };
    }

    private async Task<List<EventTypeModel>> LoadTypesAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await apiClient.GetEventTypesAsync(cancellationToken);
        }
        catch (ApiException exception)
        {
            TempData["Error"] = exception.Message;
            return [];
        }
    }

    private static EventWriteModel ToWrite(EventFormViewModel model) =>
        new(
            model.EventTypeId,
            model.Name,
            model.Description,
            model.EventDate,
            model.Location,
            model.DistanceKm,
            model.BannerImageUrl);

    private static EventFormViewModel ToForm(EventModel raceEvent) =>
        new()
        {
            EventId = raceEvent.EventId,
            EventTypeId = raceEvent.EventTypeId,
            Name = raceEvent.Name,
            Description = raceEvent.Description,
            EventDate = raceEvent.EventDate,
            Location = raceEvent.Location,
            DistanceKm = raceEvent.DistanceKm,
            BannerImageUrl = raceEvent.BannerImageUrl
        };
}

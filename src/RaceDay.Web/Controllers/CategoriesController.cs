using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Api;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;

namespace RaceDay.Web.Controllers;

[RequireLogin(Role = "Organiser")]
public sealed class CategoriesController(RaceDayApiClient apiClient) : Controller
{
    public async Task<IActionResult> Index(int eventId, CancellationToken cancellationToken)
    {
        try
        {
            var raceEvent = await apiClient.GetEventAsync(eventId, cancellationToken);
            return View(new CategoryFormViewModel
            {
                EventId = raceEvent.EventId,
                EventName = raceEvent.Name,
                Categories = raceEvent.Categories
            });
        }
        catch (ApiException exception)
        {
            TempData["Error"] = exception.Message;
            return RedirectToAction("Index", "ManageEvents");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return await ReloadAsync(model, cancellationToken);
        }

        try
        {
            await apiClient.CreateCategoryAsync(model.EventId, ToWrite(model), cancellationToken);
            TempData["Success"] = "Category added.";
            return RedirectToAction(nameof(Index), new { eventId = model.EventId });
        }
        catch (ApiException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return await ReloadAsync(model, cancellationToken);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CategoryFormViewModel model, CancellationToken cancellationToken)
    {
        if (model.CategoryId is null)
        {
            TempData["Error"] = "Choose a category to update.";
            return RedirectToAction(nameof(Index), new { eventId = model.EventId });
        }

        if (!ModelState.IsValid)
        {
            return await ReloadAsync(model, cancellationToken);
        }

        try
        {
            await apiClient.UpdateCategoryAsync(model.CategoryId.Value, ToWrite(model), cancellationToken);
            TempData["Success"] = "Category updated.";
        }
        catch (ApiException exception)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToAction(nameof(Index), new { eventId = model.EventId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int eventId, int categoryId, CancellationToken cancellationToken)
    {
        try
        {
            await apiClient.DeleteCategoryAsync(categoryId, cancellationToken);
            TempData["Success"] = "Category deleted.";
        }
        catch (ApiException exception)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToAction(nameof(Index), new { eventId });
    }

    private async Task<IActionResult> ReloadAsync(CategoryFormViewModel model, CancellationToken cancellationToken)
    {
        try
        {
            var raceEvent = await apiClient.GetEventAsync(model.EventId, cancellationToken);
            model.EventName = raceEvent.Name;
            model.Categories = raceEvent.Categories;
        }
        catch (ApiException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            model.Categories = [];
        }

        return View("Index", model);
    }

    private static CategoryWriteModel ToWrite(CategoryFormViewModel model) =>
        new(model.Name, model.MinimumAge, model.MaximumAge, model.DistanceKm);
}

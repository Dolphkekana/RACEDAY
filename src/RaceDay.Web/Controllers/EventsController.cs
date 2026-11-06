using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Api;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;

namespace RaceDay.Web.Controllers;

public sealed class EventsController(RaceDayApiClient apiClient) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        try
        {
            var raceEvent = await apiClient.GetEventAsync(id, cancellationToken);
            EnrolmentModel? existing = null;
            if (HttpContext.Session.GetString(AppSession.Role) == "Participant")
            {
                var enrolments = await apiClient.GetMyEnrolmentsAsync(cancellationToken);
                existing = enrolments.FirstOrDefault(enrolment => enrolment.EventId == id);
            }

            return View(new EventDetailViewModel
            {
                Event = raceEvent,
                ExistingEnrolment = existing,
                CategoryId = raceEvent.Categories.FirstOrDefault()?.CategoryId ?? 0
            });
        }
        catch (ApiException exception)
        {
            TempData["Error"] = exception.Message;
            return RedirectToAction("Index", "Home");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireLogin(Role = "Participant")]
    public async Task<IActionResult> Enrol(int id, int categoryId, CancellationToken cancellationToken)
    {
        try
        {
            await apiClient.EnrolAsync(id, categoryId, cancellationToken);
            TempData["Success"] = "You are entered. The organiser will confirm your place.";
        }
        catch (ApiException exception)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }
}

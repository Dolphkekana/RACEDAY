using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Api;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;

namespace RaceDay.Web.Controllers;

[RequireLogin(Role = "Organiser")]
public sealed class EnrolmentsController(RaceDayApiClient apiClient) : Controller
{
    public async Task<IActionResult> Index(int eventId, CancellationToken cancellationToken)
    {
        try
        {
            return View(await LoadAsync(eventId, cancellationToken));
        }
        catch (ApiException exception)
        {
            TempData["Error"] = exception.Message;
            return RedirectToAction("Index", "ManageEvents");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Status(int eventId, int enrolmentId, string status, CancellationToken cancellationToken)
    {
        try
        {
            await apiClient.UpdateEnrolmentStatusAsync(enrolmentId, status, cancellationToken);
            TempData["Success"] = "Enrolment status updated.";
        }
        catch (ApiException exception)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToAction(nameof(Index), new { eventId });
    }

    private async Task<EnrolmentListViewModel> LoadAsync(int eventId, CancellationToken cancellationToken)
    {
        var raceEvent = await apiClient.GetEventAsync(eventId, cancellationToken);
        var enrolments = await apiClient.GetEventEnrolmentsAsync(eventId, cancellationToken);
        return new EnrolmentListViewModel
        {
            EventId = raceEvent.EventId,
            EventName = raceEvent.Name,
            Enrolments = enrolments
        };
    }
}

using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Api;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;

namespace RaceDay.Web.Controllers;

[RequireLogin(Role = "Organiser")]
public sealed class RaceResultsController(RaceDayApiClient apiClient) : Controller
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
    public async Task<IActionResult> Save(
        int eventId,
        int enrolmentId,
        int? resultId,
        string finishTime,
        int finishingPosition,
        CancellationToken cancellationToken)
    {
        // Reject 00:00:00 and anything of 24 hours or more. The form posts hh:mm:ss.
        if (!TimeSpan.TryParse(finishTime, out var parsed) || parsed <= TimeSpan.Zero || parsed >= TimeSpan.FromDays(1))
        {
            TempData["Error"] = "Enter a finish time greater than zero and under 24 hours, such as 01:42:35.";
            return RedirectToAction(nameof(Index), new { eventId });
        }

        if (finishingPosition <= 0)
        {
            TempData["Error"] = "Finishing position must be 1 or higher.";
            return RedirectToAction(nameof(Index), new { eventId });
        }

        try
        {
            // No result id means this entry has not been published yet.
            if (resultId is null)
            {
                await apiClient.CreateResultAsync(enrolmentId, parsed, finishingPosition, cancellationToken);
                TempData["Success"] = "Result published.";
            }
            else
            {
                await apiClient.UpdateResultAsync(resultId.Value, parsed, finishingPosition, cancellationToken);
                TempData["Success"] = "Result updated.";
            }
        }
        catch (ApiException exception)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToAction(nameof(Index), new { eventId });
    }

    private async Task<ResultEntryViewModel> LoadAsync(int eventId, CancellationToken cancellationToken)
    {
        var raceEvent = await apiClient.GetEventAsync(eventId, cancellationToken);
        var enrolments = await apiClient.GetEventEnrolmentsAsync(eventId, cancellationToken);
        var results = await apiClient.GetEventResultsAsync(eventId, cancellationToken);
        var byEnrolment = results.ToDictionary(result => result.EnrolmentId);

        return new ResultEntryViewModel
        {
            EventId = raceEvent.EventId,
            EventName = raceEvent.Name,
            Rows = enrolments.Select(enrolment =>
            {
                byEnrolment.TryGetValue(enrolment.EnrolmentId, out var result);
                return new ResultEntryRow
                {
                    EnrolmentId = enrolment.EnrolmentId,
                    ParticipantName = enrolment.ParticipantName,
                    CategoryName = enrolment.CategoryName,
                    Status = enrolment.Status,
                    ResultId = result?.ResultId,
                    FinishTime = result is null ? "01:00:00" : result.FinishTime.ToString(@"hh\:mm\:ss"),
                    FinishingPosition = result?.FinishingPosition ?? 1,
                    FinisherCount = result?.FinisherCount ?? results.Count
                };
            }).ToList()
        };
    }
}

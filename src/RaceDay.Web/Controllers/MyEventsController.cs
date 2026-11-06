using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Api;
using RaceDay.Web.Filters;

namespace RaceDay.Web.Controllers;

[RequireLogin(Role = "Participant")]
public sealed class MyEventsController(RaceDayApiClient apiClient) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        try
        {
            var enrolments = await apiClient.GetMyEnrolmentsAsync(cancellationToken);
            return View(enrolments.OrderByDescending(enrolment => enrolment.EnrolmentDate).ToList());
        }
        catch (ApiException exception)
        {
            TempData["Error"] = exception.Message;
            return View(new List<EnrolmentModel>());
        }
    }
}

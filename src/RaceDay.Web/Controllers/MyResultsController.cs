using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Api;
using RaceDay.Web.Filters;

namespace RaceDay.Web.Controllers;

[RequireLogin(Role = "Participant")]
public sealed class MyResultsController(RaceDayApiClient apiClient) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        try
        {
            var results = await apiClient.GetMyResultsAsync(cancellationToken);
            return View(results);
        }
        catch (ApiException exception)
        {
            TempData["Error"] = exception.Message;
            return View(new List<ResultModel>());
        }
    }
}

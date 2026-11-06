using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Api;
using RaceDay.Web.Filters;

namespace RaceDay.Web.Controllers;

[RequireLogin(Role = "Organiser")]
public sealed class DashboardController(RaceDayApiClient apiClient) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        try
        {
            var dashboard = await apiClient.GetDashboardAsync(cancellationToken);
            return View(dashboard);
        }
        catch (ApiException exception)
        {
            TempData["Error"] = exception.Message;
            return View(new DashboardModel(0, 0, 0, []));
        }
    }
}

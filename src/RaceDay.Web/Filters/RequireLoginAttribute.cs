using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using RaceDay.Web.Api;

namespace RaceDay.Web.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequireLoginAttribute : Attribute, IFilterFactory
{
    public string? Role { get; set; }

    public bool IsReusable => false;

    public IFilterMetadata CreateInstance(IServiceProvider serviceProvider) =>
        new RequireLoginFilter(Role);

    private sealed class RequireLoginFilter(string? role) : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            var session = context.HttpContext.Session;
            var userId = session.GetInt32(AppSession.UserId);
            var currentRole = session.GetString(AppSession.Role);
            if (userId is null || string.IsNullOrEmpty(currentRole))
            {
                var returnUrl = context.HttpContext.Request.PathBase + context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
                context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
                return;
            }

            if (!string.IsNullOrEmpty(role) && !string.Equals(currentRole, role, StringComparison.Ordinal))
            {
                var tempData = context.HttpContext.RequestServices
                    .GetRequiredService<ITempDataDictionaryFactory>()
                    .GetTempData(context.HttpContext);
                tempData["Error"] = "That page is not available for your role.";
                context.Result = currentRole == "Organiser"
                    ? new RedirectToActionResult("Index", "Dashboard", null)
                    : new RedirectToActionResult("Index", "Home", null);
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
        }
    }
}

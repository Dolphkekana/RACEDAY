using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Api;
using RaceDay.Web.Models;

namespace RaceDay.Web.Controllers;

public sealed class AccountController(RaceDayApiClient apiClient) : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl)
    {
        if (IsSignedIn())
        {
            return RedirectAfterLogin(CurrentRole(), returnUrl);
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var user = await apiClient.LoginAsync(model.Email, model.Password, cancellationToken);
            Remember(user);
            return RedirectAfterLogin(user.Role, model.ReturnUrl);
        }
        catch (ApiException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return View(model);
        }
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (IsSignedIn())
        {
            return RedirectAfterLogin(CurrentRole(), null);
        }

        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await apiClient.RegisterAsync(
                model.FirstName,
                model.LastName,
                model.Email,
                model.Password,
                model.PhoneNumber,
                model.DateOfBirth,
                model.Role,
                cancellationToken);
            var user = await apiClient.LoginAsync(model.Email, model.Password, cancellationToken);
            Remember(user);
            TempData["Success"] = "Your account is ready.";
            return RedirectAfterLogin(user.Role, null);
        }
        catch (ApiException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        try
        {
            await apiClient.LogoutAsync(cancellationToken);
        }
        catch (ApiException)
        {
            // The local session is cleared either way so the browser is signed out.
        }

        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Home");
    }

    // Display fields live in the MVC session. The API cookie is saved by ApiSessionHandler during login.
    private void Remember(UserModel user)
    {
        HttpContext.Session.SetInt32(AppSession.UserId, user.UserId);
        HttpContext.Session.SetString(AppSession.Role, user.Role);
        HttpContext.Session.SetString(AppSession.DisplayName, $"{user.FirstName} {user.LastName}");
        HttpContext.Session.SetString(AppSession.Email, user.Email);
    }

    private bool IsSignedIn() => HttpContext.Session.GetInt32(AppSession.UserId) is not null;

    private string CurrentRole() => HttpContext.Session.GetString(AppSession.Role) ?? "Participant";

    private IActionResult RedirectAfterLogin(string role, string? returnUrl)
    {
        // Only same-site return addresses are followed, so a login cannot redirect off this site.
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return role == "Organiser"
            ? RedirectToAction("Index", "Dashboard")
            : RedirectToAction("Index", "Home");
    }
}

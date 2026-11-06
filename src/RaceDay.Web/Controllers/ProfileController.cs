using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Api;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;

namespace RaceDay.Web.Controllers;

[RequireLogin(Role = "Participant")]
public sealed class ProfileController(RaceDayApiClient apiClient) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        try
        {
            return View(await LoadAsync(cancellationToken));
        }
        catch (ApiException exception)
        {
            TempData["Error"] = exception.Message;
            return RedirectToAction("Index", "Home");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ProfileViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var updated = await apiClient.UpdateProfileAsync(new ProfileWriteModel(
                model.FirstName,
                model.LastName,
                model.PhoneNumber,
                model.DateOfBirth), cancellationToken);
            RememberName(updated);
            TempData["Success"] = "Profile updated.";
            return RedirectToAction(nameof(Index));
        }
        catch (ApiException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Picture(IFormFile? picture, CancellationToken cancellationToken)
    {
        if (picture is null || picture.Length == 0)
        {
            TempData["Error"] = "Choose an image to upload.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await apiClient.UploadProfilePictureAsync(picture, cancellationToken);
            TempData["Success"] = "Profile picture saved.";
        }
        catch (ApiException exception)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<ProfileViewModel> LoadAsync(CancellationToken cancellationToken)
    {
        var profile = await apiClient.GetProfileAsync(cancellationToken);
        return new ProfileViewModel
        {
            FirstName = profile.FirstName,
            LastName = profile.LastName,
            Email = profile.Email,
            Role = profile.Role,
            PhoneNumber = profile.PhoneNumber,
            DateOfBirth = profile.DateOfBirth,
            ProfileImageUrl = profile.ProfileImageUrl
        };
    }

    private void RememberName(UserModel user)
    {
        HttpContext.Session.SetString(AppSession.DisplayName, $"{user.FirstName} {user.LastName}");
    }
}

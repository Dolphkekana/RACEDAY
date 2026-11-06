using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace RaceDay.Web.Api;

public sealed class RaceDayApiClient(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<UserModel> RegisterAsync(
        string firstName,
        string lastName,
        string email,
        string password,
        string phoneNumber,
        DateTime dateOfBirth,
        string role,
        CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsJsonAsync("/api/auth/register", new
        {
            firstName,
            lastName,
            email,
            password,
            phoneNumber,
            dateOfBirth = dateOfBirth.ToString("yyyy-MM-dd"),
            role
        }, JsonOptions, cancellationToken);
        return await ReadAsync<UserModel>(response, cancellationToken);
    }

    public async Task<UserModel> LoginAsync(string email, string password, CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password
        }, JsonOptions, cancellationToken);
        return await ReadAsync<UserModel>(response, cancellationToken);
    }

    public async Task LogoutAsync(CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsync("/api/auth/logout", null, cancellationToken);
        if (response.StatusCode is HttpStatusCode.NoContent or HttpStatusCode.Unauthorized)
        {
            return;
        }

        await EnsureSuccess(response, cancellationToken);
    }

    public Task<UserModel> GetProfileAsync(CancellationToken cancellationToken) =>
        GetAsync<UserModel>("/api/users/me", cancellationToken);

    public async Task<UserModel> UpdateProfileAsync(ProfileWriteModel profile, CancellationToken cancellationToken)
    {
        var response = await httpClient.PutAsJsonAsync("/api/users/me", new
        {
            profile.FirstName,
            profile.LastName,
            profile.PhoneNumber,
            dateOfBirth = profile.DateOfBirth.ToString("yyyy-MM-dd")
        }, JsonOptions, cancellationToken);
        return await ReadAsync<UserModel>(response, cancellationToken);
    }

    public async Task<UserModel> UploadProfilePictureAsync(IFormFile file, CancellationToken cancellationToken)
    {
        using var content = CreateFileContent(file);
        var response = await httpClient.PostAsync("/api/users/me/picture", content, cancellationToken);
        return await ReadAsync<UserModel>(response, cancellationToken);
    }

    public async Task<List<EventModel>> GetEventsAsync(
        string? name,
        int? eventTypeId,
        string? location,
        bool upcoming,
        decimal? minDistance,
        decimal? maxDistance,
        CancellationToken cancellationToken)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(name))
        {
            query.Add($"name={Uri.EscapeDataString(name.Trim())}");
        }

        if (eventTypeId.HasValue)
        {
            query.Add($"eventTypeId={eventTypeId.Value}");
        }

        if (!string.IsNullOrWhiteSpace(location))
        {
            query.Add($"location={Uri.EscapeDataString(location.Trim())}");
        }

        if (upcoming)
        {
            query.Add("upcoming=true");
        }

        if (minDistance.HasValue)
        {
            query.Add($"minDistance={minDistance.Value}");
        }

        if (maxDistance.HasValue)
        {
            query.Add($"maxDistance={maxDistance.Value}");
        }

        var path = query.Count == 0 ? "/api/events" : "/api/events?" + string.Join("&", query);
        return await GetAsync<List<EventModel>>(path, cancellationToken);
    }

    public Task<EventModel> GetEventAsync(int eventId, CancellationToken cancellationToken) =>
        GetAsync<EventModel>($"/api/events/{eventId}", cancellationToken);

    public Task<List<EventTypeModel>> GetEventTypesAsync(CancellationToken cancellationToken) =>
        GetAsync<List<EventTypeModel>>("/api/event-types", cancellationToken);

    public Task<List<EventModel>> GetMyEventsAsync(CancellationToken cancellationToken) =>
        GetAsync<List<EventModel>>("/api/events/mine", cancellationToken);

    public async Task<EventModel> CreateEventAsync(EventWriteModel model, CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsJsonAsync("/api/events", model, JsonOptions, cancellationToken);
        return await ReadAsync<EventModel>(response, cancellationToken);
    }

    public async Task<EventModel> UpdateEventAsync(int eventId, EventWriteModel model, CancellationToken cancellationToken)
    {
        var response = await httpClient.PutAsJsonAsync($"/api/events/{eventId}", model, JsonOptions, cancellationToken);
        return await ReadAsync<EventModel>(response, cancellationToken);
    }

    public async Task DeleteEventAsync(int eventId, CancellationToken cancellationToken)
    {
        var response = await httpClient.DeleteAsync($"/api/events/{eventId}", cancellationToken);
        await EnsureSuccess(response, cancellationToken);
    }

    public async Task<EventModel> UploadBannerAsync(int eventId, IFormFile file, CancellationToken cancellationToken)
    {
        using var content = CreateFileContent(file);
        var response = await httpClient.PostAsync($"/api/events/{eventId}/banner", content, cancellationToken);
        return await ReadAsync<EventModel>(response, cancellationToken);
    }

    public async Task<CategoryModel> CreateCategoryAsync(
        int eventId,
        CategoryWriteModel model,
        CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsJsonAsync($"/api/events/{eventId}/categories", model, JsonOptions, cancellationToken);
        return await ReadAsync<CategoryModel>(response, cancellationToken);
    }

    public async Task<CategoryModel> UpdateCategoryAsync(
        int categoryId,
        CategoryWriteModel model,
        CancellationToken cancellationToken)
    {
        var response = await httpClient.PutAsJsonAsync($"/api/categories/{categoryId}", model, JsonOptions, cancellationToken);
        return await ReadAsync<CategoryModel>(response, cancellationToken);
    }

    public async Task DeleteCategoryAsync(int categoryId, CancellationToken cancellationToken)
    {
        var response = await httpClient.DeleteAsync($"/api/categories/{categoryId}", cancellationToken);
        await EnsureSuccess(response, cancellationToken);
    }

    public async Task<EnrolmentModel> EnrolAsync(int eventId, int categoryId, CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsJsonAsync(
            $"/api/events/{eventId}/enrolments",
            new { categoryId },
            JsonOptions,
            cancellationToken);
        return await ReadAsync<EnrolmentModel>(response, cancellationToken);
    }

    public Task<List<EnrolmentModel>> GetMyEnrolmentsAsync(CancellationToken cancellationToken) =>
        GetAsync<List<EnrolmentModel>>("/api/enrolments/my", cancellationToken);

    public Task<List<EnrolmentModel>> GetEventEnrolmentsAsync(int eventId, CancellationToken cancellationToken) =>
        GetAsync<List<EnrolmentModel>>($"/api/events/{eventId}/enrolments", cancellationToken);

    public async Task<EnrolmentModel> UpdateEnrolmentStatusAsync(
        int enrolmentId,
        string status,
        CancellationToken cancellationToken)
    {
        var response = await httpClient.PutAsJsonAsync(
            $"/api/enrolments/{enrolmentId}/status",
            new { status },
            JsonOptions,
            cancellationToken);
        return await ReadAsync<EnrolmentModel>(response, cancellationToken);
    }

    public Task<List<ResultModel>> GetMyResultsAsync(CancellationToken cancellationToken) =>
        GetAsync<List<ResultModel>>("/api/results/my", cancellationToken);

    public Task<List<ResultModel>> GetEventResultsAsync(int eventId, CancellationToken cancellationToken) =>
        GetAsync<List<ResultModel>>($"/api/events/{eventId}/results", cancellationToken);

    public async Task<ResultModel> CreateResultAsync(
        int enrolmentId,
        TimeSpan finishTime,
        int finishingPosition,
        CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsJsonAsync(
            $"/api/enrolments/{enrolmentId}/result",
            // Always send hh:mm:ss. A value such as 45:10 would be read as 45 hours.
            new { finishTime = finishTime.ToString(@"hh\:mm\:ss"), finishingPosition },
            JsonOptions,
            cancellationToken);
        return await ReadAsync<ResultModel>(response, cancellationToken);
    }

    public async Task<ResultModel> UpdateResultAsync(
        int resultId,
        TimeSpan finishTime,
        int finishingPosition,
        CancellationToken cancellationToken)
    {
        var response = await httpClient.PutAsJsonAsync(
            $"/api/results/{resultId}",
            new { finishTime = finishTime.ToString(@"hh\:mm\:ss"), finishingPosition },
            JsonOptions,
            cancellationToken);
        return await ReadAsync<ResultModel>(response, cancellationToken);
    }

    public Task<DashboardModel> GetDashboardAsync(CancellationToken cancellationToken) =>
        GetAsync<DashboardModel>("/api/organisers/dashboard", cancellationToken);

    private async Task<T> GetAsync<T>(string path, CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync(path, cancellationToken);
        return await ReadAsync<T>(response, cancellationToken);
    }

    private static MultipartFormDataContent CreateFileContent(IFormFile file)
    {
        var content = new MultipartFormDataContent();
        var stream = file.OpenReadStream();
        var part = new StreamContent(stream);
        if (!string.IsNullOrWhiteSpace(file.ContentType))
        {
            part.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        }

        content.Add(part, "file", string.IsNullOrWhiteSpace(file.FileName) ? "image.jpg" : file.FileName);
        return content;
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        await EnsureSuccess(response, cancellationToken);
        var body = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
        return body ?? throw new ApiException((int)response.StatusCode, "The API returned an empty response.");
    }

    private static async Task EnsureSuccess(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        // Problem details from the API become the message shown on the page, not a raw status code.

        throw new ApiException((int)response.StatusCode, await ReadErrorAsync(response, cancellationToken));
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var text = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(text))
        {
            try
            {
                using var document = JsonDocument.Parse(text);
                var root = document.RootElement;
                if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                {
                    var messages = new List<string>();
                    foreach (var property in errors.EnumerateObject())
                    {
                        if (property.Value.ValueKind != JsonValueKind.Array)
                        {
                            continue;
                        }

                        foreach (var item in property.Value.EnumerateArray())
                        {
                            var message = item.GetString();
                            if (!string.IsNullOrWhiteSpace(message))
                            {
                                messages.Add(message);
                            }
                        }
                    }

                    if (messages.Count > 0)
                    {
                        return string.Join(" ", messages);
                    }
                }

                var title = root.TryGetProperty("title", out var titleElement) ? titleElement.GetString() : null;
                var detail = root.TryGetProperty("detail", out var detailElement) ? detailElement.GetString() : null;
                var combined = string.Join(" ", new[] { title, detail }.Where(part => !string.IsNullOrWhiteSpace(part)));
                if (!string.IsNullOrWhiteSpace(combined))
                {
                    return combined;
                }
            }
            catch (JsonException)
            {
                // Fall through to a generic message when the body is not problem JSON.
            }
        }

        return response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "Sign in to continue.",
            HttpStatusCode.Forbidden => "You do not have access to that action.",
            HttpStatusCode.NotFound => "That record could not be found.",
            _ => "The request could not be completed."
        };
    }
}

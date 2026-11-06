namespace RaceDay.Web.Api;

// Forwards the API session cookie stored in the MVC session, and keeps it updated.
public sealed class ApiSessionHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    private const string CookieName = "RaceDay.Session";

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var session = httpContextAccessor.HttpContext?.Session;
        var cookie = session?.GetString(AppSession.ApiCookie);
        if (!string.IsNullOrWhiteSpace(cookie))
        {
            request.Headers.Remove("Cookie");
            request.Headers.TryAddWithoutValidation("Cookie", $"{CookieName}={cookie}");
        }

        var response = await base.SendAsync(request, cancellationToken);
        if (session is null || response.Headers.TryGetValues("Set-Cookie", out var values) == false)
        {
            return response;
        }

        foreach (var value in values)
        {
            var pair = value.Split(';', 2)[0];
            if (!pair.StartsWith(CookieName + "=", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var cookieValue = pair[(CookieName.Length + 1)..];
            if (string.IsNullOrEmpty(cookieValue))
            {
                session.Remove(AppSession.ApiCookie);
            }
            else
            {
                session.SetString(AppSession.ApiCookie, cookieValue);
            }
        }

        return response;
    }
}

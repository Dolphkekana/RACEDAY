namespace RaceDay.Web;

public static class MediaUrls
{
    public static string? ForBrowser(HttpContext httpContext, string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return url;
        }

        var configuration = httpContext.RequestServices.GetRequiredService<IConfiguration>();
        var publicBase = configuration["Api:PublicBaseUrl"];
        if (string.IsNullOrWhiteSpace(publicBase))
        {
            publicBase = configuration["Api:BaseUrl"];
        }

        return string.IsNullOrWhiteSpace(publicBase)
            ? url
            : publicBase.TrimEnd('/') + "/" + url.TrimStart('/');
    }
}

namespace RaceDay.Api.Services;

// Keeps uploaded images on disk and serves them from the API when Azure is not configured.
public sealed class LocalFileImageStorage(IWebHostEnvironment environment) : IImageStorage
{
    public async Task<string> UploadAsync(
        string container,
        string fileName,
        Stream content,
        string contentType,
        CancellationToken cancellationToken)
    {
        var safeContainer = SanitizeSegment(container);
        var safeFileName = SanitizeSegment(fileName);
        var directory = Path.Combine(environment.ContentRootPath, "App_Data", "images", safeContainer);
        Directory.CreateDirectory(directory);

        var path = Path.Combine(directory, safeFileName);
        await using var output = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        await content.CopyToAsync(output, cancellationToken);

        return $"/api/media/{safeContainer}/{safeFileName}";
    }

    public static string? ResolvePath(IWebHostEnvironment environment, string container, string fileName)
    {
        if (!IsSafeSegment(container) || !IsSafeSegment(fileName))
        {
            return null;
        }

        var root = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "App_Data", "images"));
        var path = Path.GetFullPath(Path.Combine(root, container, fileName));
        // Reject names that resolve outside the image folder.
        if (!path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            return null;
        }

        return File.Exists(path) ? path : null;
    }

    private static string SanitizeSegment(string value)
    {
        var cleaned = new string(value.Where(character =>
            char.IsLetterOrDigit(character) || character is '.' or '-' or '_').ToArray());
        if (string.IsNullOrWhiteSpace(cleaned) || cleaned is "." or "..")
        {
            throw new InvalidOperationException("Image name is not valid.");
        }

        return cleaned;
    }

    private static bool IsSafeSegment(string value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.All(character => char.IsLetterOrDigit(character) || character is '.' or '-' or '_')
        && value is not "." and not "..";
}

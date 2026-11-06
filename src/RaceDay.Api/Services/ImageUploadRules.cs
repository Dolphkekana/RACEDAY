namespace RaceDay.Api.Services;

public static class ImageUploadRules
{
    public const long MaxBytes = 5 * 1024 * 1024;

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/gif"
    };

    private static readonly Dictionary<string, string> ExtensionByContentType = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp",
        ["image/gif"] = ".gif"
    };

    public static string? Validate(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            return "Choose an image to upload.";
        }

        if (file.Length > MaxBytes)
        {
            return "Images must be 5 MB or smaller.";
        }

        if (string.IsNullOrWhiteSpace(file.ContentType) || !AllowedContentTypes.Contains(file.ContentType))
        {
            return "Upload a JPG, PNG, WEBP, or GIF image.";
        }

        return null;
    }

    // The stored name comes from the content type, not the uploaded file name, so it cannot carry a path.
    public static string CreateFileName(string contentType)
    {
        var extension = ExtensionByContentType.TryGetValue(contentType, out var known)
            ? known
            : ".img";
        return $"{Guid.NewGuid():N}{extension}";
    }
}

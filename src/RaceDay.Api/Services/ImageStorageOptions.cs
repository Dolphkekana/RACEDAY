namespace RaceDay.Api.Services;

public sealed class ImageStorageOptions
{
    public const string SectionName = "AzureStorage";

    // Azure Storage or Azurite connection string. Empty means files stay on local disk.
    public string? ConnectionString { get; set; }

    public string ContainerName { get; set; } = "raceday-images";

    // When set, blob URLs are rewritten so a browser can open them.
    // Used for Azurite, where the API reaches the emulator on an internal host.
    public string? PublicBlobBaseUrl { get; set; }
}

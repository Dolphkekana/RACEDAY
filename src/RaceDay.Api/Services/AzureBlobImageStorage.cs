using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;

namespace RaceDay.Api.Services;

// Uploads images with the Azure Blob Storage SDK. The same client works against Azurite.
public sealed class AzureBlobImageStorage : IImageStorage
{
    private readonly BlobContainerClient containerClient;
    private readonly string? publicBlobBaseUrl;
    private readonly string containerName;

    public AzureBlobImageStorage(IOptions<ImageStorageOptions> options)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.ConnectionString))
        {
            throw new InvalidOperationException("Azure Storage connection string is missing.");
        }

        containerName = string.IsNullOrWhiteSpace(settings.ContainerName)
            ? "raceday-images"
            : settings.ContainerName.Trim();
        publicBlobBaseUrl = string.IsNullOrWhiteSpace(settings.PublicBlobBaseUrl)
            ? null
            : settings.PublicBlobBaseUrl.TrimEnd('/');
        containerClient = new BlobContainerClient(settings.ConnectionString, containerName);
    }

    public async Task<string> UploadAsync(
        string container,
        string fileName,
        Stream content,
        string contentType,
        CancellationToken cancellationToken)
    {
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);

        var blobName = $"{container}/{fileName}";
        var blobClient = containerClient.GetBlobClient(blobName);
        await blobClient.UploadAsync(
            content,
            new BlobHttpHeaders { ContentType = contentType },
            cancellationToken: cancellationToken);

        if (publicBlobBaseUrl is null)
        {
            return blobClient.Uri.ToString();
        }

        return $"{publicBlobBaseUrl}/{containerName}/{blobName}";
    }
}

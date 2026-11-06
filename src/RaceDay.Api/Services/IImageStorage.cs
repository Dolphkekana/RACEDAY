namespace RaceDay.Api.Services;

// Stores an image and returns the URL that should be saved on the related record.
public interface IImageStorage
{
    Task<string> UploadAsync(
        string container,
        string fileName,
        Stream content,
        string contentType,
        CancellationToken cancellationToken);
}

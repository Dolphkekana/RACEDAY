using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/media")]
public sealed class MediaController(IWebHostEnvironment environment) : ControllerBase
{
    [SwaggerOperation(Summary = "Download a locally stored image", Description = "Public. Returns an image saved by the local storage fallback. Azure Blob URLs are opened directly and do not use this route.")]
    [HttpGet("{container}/{fileName}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Get(string container, string fileName)
    {
        var path = LocalFileImageStorage.ResolvePath(environment, container, fileName);
        if (path is null)
        {
            return NotFound();
        }

        var contentType = Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            _ => "image/jpeg"
        };
        return PhysicalFile(path, contentType);
    }
}

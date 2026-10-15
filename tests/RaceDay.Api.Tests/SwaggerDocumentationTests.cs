using System.Net;
using System.Text.Json;

namespace RaceDay.Api.Tests;

/*
 Checks that the Swagger document is generated and that every endpoint
 has a summary and documents the status codes it can return.
*/
public sealed class SwaggerDocumentationTests : IClassFixture<EventsCategoriesApplicationFactory>
{
    private readonly EventsCategoriesApplicationFactory factory;

    public SwaggerDocumentationTests(EventsCategoriesApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task SwaggerJson_DocumentsEveryEndpointWithSummaryAndResponses()
    {
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var paths = document.RootElement.GetProperty("paths");
        Assert.True(paths.TryGetProperty("/api/events/mine", out _));
        Assert.True(paths.TryGetProperty("/api/events/{eventId}/results", out _));

        foreach (var path in paths.EnumerateObject())
        {
            foreach (var operation in path.Value.EnumerateObject())
            {
                // Each operation needs a summary and at least one documented response
                Assert.True(operation.Value.TryGetProperty("summary", out var summary), path.Name);
                Assert.False(string.IsNullOrWhiteSpace(summary.GetString()), path.Name);
                Assert.True(operation.Value.GetProperty("responses").EnumerateObject().Any(), path.Name);
            }
        }
    }
}

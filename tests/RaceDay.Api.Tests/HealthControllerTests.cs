using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Contracts;
using RaceDay.Api.Controllers;
using Xunit;

namespace RaceDay.Api.Tests;


public sealed class HealthControllerTests
{
    [Fact]
    public void GetHealth_ReturnsOkResult_WithHealthyStatus()
    {
        // Arrange
        var controller = new HealthController();

        // Act 
        var result = controller.GetHealth();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<HealthResponse>(okResult.Value);
        Assert.Equal("Healthy", response.Status);
        Assert.Equal("RaceDay API", response.Service);
        Assert.True(response.Timestamp > DateTimeOffset.UtcNow.AddMinutes(-1));
    }
}

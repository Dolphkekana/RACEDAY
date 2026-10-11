using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Controllers;
using Xunit;

namespace RaceDay.Api.Tests;


 // Unit tests verifying the HealthController foundation endpoint

public class HealthControllerTests
{
    [Fact]
    public void GetHealth_ReturnsOkResult_WithHealthyStatus()
    {
        // Arrange
        var controller = new HealthController();

        // Act 
        var result = controller.GetHealth();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }
}

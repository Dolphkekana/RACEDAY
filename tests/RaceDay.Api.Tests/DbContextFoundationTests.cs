using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using Xunit;

namespace RaceDay.Api.Tests;

/*
 Unit tests verifying DbContext entity model mappings and basic database operations.
 Uses EF Core In-Memory database provider for fast, isolated test execution.
*/
public class DbContextFoundationTests
{
    private DbContextOptions<RaceDayDbContext> CreateNewContextOptions()
    {
        return new DbContextOptionsBuilder<RaceDayDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task CanInsertAndRetrieveUserAsync()
    {
        // Arrange 
        var options = CreateNewContextOptions();
        using var context = new RaceDayDbContext(options);



        var user = new User
        {
            FirstName = "Test",
            LastName = "Organiser",
            Email = "organiser@raceday.co.za",
            PasswordHash = "HashedPassword123",
            PhoneNumber = "0712345678",
            DateOfBirth = new DateTime(1990, 1, 1),
            Role = "Organiser"
        };


        // Act 
        context.Users.Add(user);
        await context.SaveChangesAsync();


        // Assert 
        var retrievedUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "organiser@raceday.co.za");
        Assert.NotNull(retrievedUser);
        Assert.Equal("Test", retrievedUser.FirstName);
        Assert.Equal("Organiser", retrievedUser.Role);
    }


    [Fact]
    public async Task CanSeedAndQueryEventTypesAsync()
    {
        // Arrange 
        var options = CreateNewContextOptions();
        using var context = new RaceDayDbContext(options);

        context.EventTypes.AddRange(
            new EventType { Name = "Run" },
            new EventType { Name = "Walk" },
            new EventType { Name = "Cycle" }
        );
        await context.SaveChangesAsync();

        // Act
        var count = await context.EventTypes.CountAsync();

        // Assert
        Assert.Equal(3, count);
    }
}

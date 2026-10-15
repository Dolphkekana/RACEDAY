using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RaceDay.Api.Data;
using RaceDay.Api.Models;

namespace RaceDay.Api.Tests;

public sealed class EventsCategoriesIntegrationTests
    : IClassFixture<EventsCategoriesApplicationFactory>
{
    private readonly EventsCategoriesApplicationFactory factory;

    public EventsCategoriesIntegrationTests(EventsCategoriesApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task PublicUsersCanListAndRetrieveEventsAndCategories()
    {
        using var client = factory.CreateClient();
        var eventId = await CreateEventAsync(client);
        var categoryResponse = await client.PostAsJsonAsync(
            $"/api/events/{eventId}/categories",
            new { name = "Open", minimumAge = 18, maximumAge = 65, distanceKm = 10 });
        Assert.Equal(HttpStatusCode.Created, categoryResponse.StatusCode);

        var list = await client.GetAsync("/api/events");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Contains(eventId.ToString(), await list.Content.ReadAsStringAsync());

        var details = await client.GetAsync($"/api/events/{eventId}");
        Assert.Equal(HttpStatusCode.OK, details.StatusCode);
        Assert.Contains("Open", await details.Content.ReadAsStringAsync());

        var categories = await client.GetAsync($"/api/events/{eventId}/categories");
        Assert.Equal(HttpStatusCode.OK, categories.StatusCode);
    }

    [Fact]
    public async Task MissingEventAndCategoryReturnNotFound()
    {
        using var client = factory.CreateClient();

        Assert.Equal(
            HttpStatusCode.NotFound,
            (await client.GetAsync("/api/events/99999")).StatusCode);
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await client.GetAsync("/api/events/99999/categories")).StatusCode);
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await client.GetAsync("/api/categories/99999")).StatusCode);
    }

    [Fact]
    public async Task AnonymousAndParticipantCannotWriteEventsOrCategories()
    {
        using var anonymousClient = factory.CreateClient();
        var eventRequest = CreateEventRequest();
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await anonymousClient.PostAsJsonAsync("/api/events", eventRequest)).StatusCode);

        using var participantClient = factory.CreateClient();
        await LoginAsync(participantClient, "participant@example.com");
        var participantResponse = await participantClient.PostAsJsonAsync("/api/events", eventRequest);
        Assert.Equal(HttpStatusCode.Forbidden, participantResponse.StatusCode);
    }

    [Fact]
    public async Task OrganiserCanCreateUpdateAndDeleteEventAndCategory()
    {
        using var client = factory.CreateClient();
        await LoginAsync(client, "organiser@example.com");

        var create = await client.PostAsJsonAsync("/api/events", CreateEventRequest());
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await create.Content.ReadFromJsonAsync<EventPayload>();
        Assert.NotNull(created);

        var update = await client.PutAsJsonAsync(
            $"/api/events/{created.EventId}",
            CreateEventRequest(name: "Updated Race"));
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var categoryCreate = await client.PostAsJsonAsync(
            $"/api/events/{created.EventId}/categories",
            new { name = "Senior", minimumAge = 18, maximumAge = 35, distanceKm = 10 });
        Assert.Equal(HttpStatusCode.Created, categoryCreate.StatusCode);
        var category = await categoryCreate.Content.ReadFromJsonAsync<CategoryPayload>();
        Assert.NotNull(category);

        var categoryUpdate = await client.PutAsJsonAsync(
            $"/api/categories/{category.CategoryId}",
            new { name = "Senior Updated", minimumAge = 18, maximumAge = 40, distanceKm = 10 });
        Assert.Equal(HttpStatusCode.OK, categoryUpdate.StatusCode);

        Assert.Equal(
            HttpStatusCode.Conflict,
            (await client.DeleteAsync($"/api/events/{created.EventId}")).StatusCode);
        Assert.Equal(
            HttpStatusCode.NoContent,
            (await client.DeleteAsync($"/api/categories/{category.CategoryId}")).StatusCode);
        Assert.Equal(
            HttpStatusCode.NoContent,
            (await client.DeleteAsync($"/api/events/{created.EventId}")).StatusCode);
    }

    [Fact]
    public async Task InvalidCategoryRangeAndMissingEventAreRejected()
    {
        using var client = factory.CreateClient();
        await LoginAsync(client, "organiser@example.com");

        var invalid = await client.PostAsJsonAsync(
            "/api/events/99999/categories",
            new { name = "Invalid", minimumAge = 60, maximumAge = 18 });
        Assert.Equal(HttpStatusCode.NotFound, invalid.StatusCode);

        var eventId = await CreateEventAsync(client);
        invalid = await client.PostAsJsonAsync(
            $"/api/events/{eventId}/categories",
            new { name = "Invalid", minimumAge = 60, maximumAge = 18 });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
    }

    [Fact]
    public async Task EnrolmentsPreventEventAndCategoryDeletion()
    {
        using var client = factory.CreateClient();
        await LoginAsync(client, "organiser@example.com");
        var eventId = await CreateEventAsync(client);
        var categoryResponse = await client.PostAsJsonAsync(
            $"/api/events/{eventId}/categories",
            new { name = "Used", distanceKm = 10 });
        var category = await categoryResponse.Content.ReadFromJsonAsync<CategoryPayload>();
        Assert.NotNull(category);

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
            dbContext.Enrolments.Add(new Enrolment
            {
                ParticipantId = 1,
                EventId = eventId,
                CategoryId = category.CategoryId
            });
            await dbContext.SaveChangesAsync();
        }

        Assert.Equal(
            HttpStatusCode.Conflict,
            (await client.DeleteAsync($"/api/categories/{category.CategoryId}")).StatusCode);
        Assert.Equal(
            HttpStatusCode.Conflict,
            (await client.DeleteAsync($"/api/events/{eventId}")).StatusCode);
    }

    private async Task<int> CreateEventAsync(HttpClient client)
    {
        await LoginAsync(client, "organiser@example.com");
        var response = await client.PostAsJsonAsync("/api/events", CreateEventRequest());
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<EventPayload>();
        Assert.NotNull(created);
        return created.EventId;
    }

    private static async Task LoginAsync(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "SecurePass1"
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static object CreateEventRequest(string name = "Test Race") => new
    {
        eventTypeId = 1,
        name,
        description = "A test event",
        eventDate = DateTime.UtcNow.AddDays(30).Date,
        location = "Johannesburg",
        distanceKm = 10.00m
    };

    private sealed record EventPayload(int EventId);
    private sealed record CategoryPayload(int CategoryId);
}

public sealed class EventsCategoriesApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string databaseName = $"RaceDayEventsTests-{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<RaceDayDbContext>>();
            services.AddDbContext<RaceDayDbContext>(options =>
                options.UseInMemoryDatabase(databaseName));

            using var scope = services.BuildServiceProvider().CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
            dbContext.Database.EnsureCreated();
            dbContext.Users.AddRange(
                TestUserFactory.Create("Participant", "participant@example.com", 1),
                TestUserFactory.Create("Organiser", "organiser@example.com", 2));
            dbContext.SaveChanges();
        });
    }
}

internal static class TestUserFactory
{
    public static User Create(string role, string email, int userId)
    {
        var user = new User
        {
            UserId = userId,
            FirstName = role,
            LastName = "Test",
            Email = email,
            PhoneNumber = "0712345678",
            DateOfBirth = new DateTime(1990, 1, 1),
            Role = role
        };
        user.PasswordHash = new Microsoft.AspNetCore.Identity.PasswordHasher<User>()
            .HashPassword(user, "SecurePass1");
        return user;
    }
}

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

[Collection("Enrolment tests")]
public sealed class EnrolmentsIntegrationTests
    : IClassFixture<EnrolmentsApplicationFactory>, IAsyncLifetime
{
    private readonly EnrolmentsApplicationFactory factory;

    public EnrolmentsIntegrationTests(EnrolmentsApplicationFactory factory)
    {
        this.factory = factory;
    }

    public async Task InitializeAsync()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
        dbContext.Enrolments.RemoveRange(dbContext.Enrolments);
        dbContext.Categories.RemoveRange(dbContext.Categories);
        dbContext.Events.RemoveRange(dbContext.Events);
        await dbContext.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ParticipantCanEnrolAndRetrieveOwnEnrolment()
    {
        var (eventId, categoryId) = await CreateEventAndCategoryAsync();
        using var client = await LoginClientAsync("participant@example.com");

        var create = await client.PostAsJsonAsync(
            $"/api/events/{eventId}/enrolments",
            new { categoryId });

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var enrolment = await create.Content.ReadFromJsonAsync<EnrolmentPayload>();
        Assert.NotNull(enrolment);
        Assert.Equal("Pending", enrolment.Status);

        var mine = await client.GetAsync("/api/enrolments/my");
        Assert.Equal(HttpStatusCode.OK, mine.StatusCode);
        Assert.Contains(enrolment.EnrolmentId.ToString(), await mine.Content.ReadAsStringAsync());

        var details = await client.GetAsync($"/api/enrolments/{enrolment.EnrolmentId}");
        Assert.Equal(HttpStatusCode.OK, details.StatusCode);
    }

    [Fact]
    public async Task EnrolmentRequiresParticipantAuthentication()
    {
        var (eventId, categoryId) = await CreateEventAndCategoryAsync();
        using var anonymous = factory.CreateClient();
        using var organiser = await LoginClientAsync("organiser@example.com");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await anonymous.PostAsJsonAsync(
                $"/api/events/{eventId}/enrolments",
                new { categoryId })).StatusCode);
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await organiser.PostAsJsonAsync(
                $"/api/events/{eventId}/enrolments",
                new { categoryId })).StatusCode);
    }

    [Fact]
    public async Task ParticipantCannotSubmitAnotherUserIdOrRetrieveAnotherParticipantsEnrolment()
    {
        var (eventId, categoryId) = await CreateEventAndCategoryAsync();
        using var firstParticipant = await LoginClientAsync("participant@example.com");
        var create = await firstParticipant.PostAsJsonAsync(
            $"/api/events/{eventId}/enrolments",
            new { categoryId, participantId = 999 });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var enrolment = await create.Content.ReadFromJsonAsync<EnrolmentPayload>();
        Assert.NotNull(enrolment);
        Assert.Equal(1, enrolment.ParticipantId);

        using var secondParticipant = await LoginClientAsync("second.participant@example.com");
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await secondParticipant.GetAsync(
                $"/api/enrolments/{enrolment.EnrolmentId}")).StatusCode);
    }

    [Fact]
    public async Task MissingAndMismatchedReferencesAreRejected()
    {
        using var participant = await LoginClientAsync("participant@example.com");
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await participant.PostAsJsonAsync(
                "/api/events/99999/enrolments",
                new { categoryId = 1 })).StatusCode);

        var (eventId, _) = await CreateEventAndCategoryAsync();
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await participant.PostAsJsonAsync(
                $"/api/events/{eventId}/enrolments",
                new { categoryId = 99999 })).StatusCode);

        var otherEvent = await CreateEventAndCategoryAsync();
        Assert.Equal(
            HttpStatusCode.Conflict,
            (await participant.PostAsJsonAsync(
                $"/api/events/{eventId}/enrolments",
                new { categoryId = otherEvent.categoryId })).StatusCode);
    }

    [Fact]
    public async Task DuplicateEnrolmentIsRejected()
    {
        var (eventId, categoryId) = await CreateEventAndCategoryAsync();
        using var participant = await LoginClientAsync("participant@example.com");
        var request = new { categoryId };

        Assert.Equal(
            HttpStatusCode.Created,
            (await participant.PostAsJsonAsync(
                $"/api/events/{eventId}/enrolments", request)).StatusCode);
        Assert.Equal(
            HttpStatusCode.Conflict,
            (await participant.PostAsJsonAsync(
                $"/api/events/{eventId}/enrolments", request)).StatusCode);
    }

    [Fact]
    public async Task OwningOrganiserCanViewAndUpdateEventEnrolments()
    {
        var (eventId, categoryId) = await CreateEventAndCategoryAsync();
        using var participant = await LoginClientAsync("participant@example.com");
        var create = await participant.PostAsJsonAsync(
            $"/api/events/{eventId}/enrolments",
            new { categoryId });
        var enrolment = await create.Content.ReadFromJsonAsync<EnrolmentPayload>();
        Assert.NotNull(enrolment);

        using var organiser = await LoginClientAsync("organiser@example.com");
        var list = await organiser.GetAsync($"/api/events/{eventId}/enrolments");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Contains(enrolment.EnrolmentId.ToString(), await list.Content.ReadAsStringAsync());

        var update = await organiser.PutAsJsonAsync(
            $"/api/enrolments/{enrolment.EnrolmentId}/status",
            new { status = "Confirmed" });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            (await organiser.PutAsJsonAsync(
                $"/api/enrolments/{enrolment.EnrolmentId}/status",
                new { status = "Approved" })).StatusCode);
    }

    [Fact]
    public async Task OrganiserCannotManageAnotherOrganisersEvent()
    {
        var (eventId, categoryId) = await CreateEventAndCategoryAsync();
        using var participant = await LoginClientAsync("participant@example.com");
        await participant.PostAsJsonAsync(
            $"/api/events/{eventId}/enrolments",
            new { categoryId });

        using var otherOrganiser = await LoginClientAsync("second.organiser@example.com");
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await otherOrganiser.GetAsync($"/api/events/{eventId}/enrolments")).StatusCode);

        var enrolmentId = await GetOnlyEnrolmentIdAsync();
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await otherOrganiser.PutAsJsonAsync(
                $"/api/enrolments/{enrolmentId}/status",
                new { status = "Cancelled" })).StatusCode);
    }

    [Fact]
    public async Task MissingEnrolmentReturnsNotFound()
    {
        using var participant = await LoginClientAsync("participant@example.com");
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await participant.GetAsync("/api/enrolments/99999")).StatusCode);

        using var organiser = await LoginClientAsync("organiser@example.com");
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await organiser.PutAsJsonAsync(
                "/api/enrolments/99999/status",
                new { status = "Confirmed" })).StatusCode);
    }

    private async Task<(int eventId, int categoryId)> CreateEventAndCategoryAsync()
    {
        using var organiser = await LoginClientAsync("organiser@example.com");
        var eventResponse = await organiser.PostAsJsonAsync("/api/events", new
        {
            eventTypeId = 1,
            name = $"Enrolment Race {Guid.NewGuid():N}",
            description = "Enrolment integration test event",
            eventDate = DateTime.UtcNow.AddDays(30).Date,
            location = "Johannesburg",
            distanceKm = 10.00m
        });
        Assert.Equal(HttpStatusCode.Created, eventResponse.StatusCode);
        var raceEvent = await eventResponse.Content.ReadFromJsonAsync<EventPayload>();
        Assert.NotNull(raceEvent);

        var categoryResponse = await organiser.PostAsJsonAsync(
            $"/api/events/{raceEvent.EventId}/categories",
            new { name = $"Open {Guid.NewGuid():N}", distanceKm = 10.00m });
        Assert.Equal(HttpStatusCode.Created, categoryResponse.StatusCode);
        var category = await categoryResponse.Content.ReadFromJsonAsync<CategoryPayload>();
        Assert.NotNull(category);
        return (raceEvent.EventId, category.CategoryId);
    }

    private async Task<int> GetOnlyEnrolmentIdAsync()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
        return await dbContext.Enrolments.Select(enrolment => enrolment.EnrolmentId).SingleAsync();
    }

    private async Task<HttpClient> LoginClientAsync(string email)
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "SecurePass1"
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return client;
    }

    private sealed record EventPayload(int EventId);
    private sealed record CategoryPayload(int CategoryId);
    private sealed record EnrolmentPayload(
        int EnrolmentId,
        int ParticipantId,
        string Status);
}

public sealed class EnrolmentsApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string databaseName = $"RaceDayEnrolmentTests-{Guid.NewGuid():N}";

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
                TestUserFactory.Create("Organiser", "organiser@example.com", 2),
                TestUserFactory.Create("Participant", "second.participant@example.com", 3),
                TestUserFactory.Create("Organiser", "second.organiser@example.com", 4));
            dbContext.SaveChanges();
        });
    }
}

[CollectionDefinition("Enrolment tests", DisableParallelization = true)]
public sealed class EnrolmentTestsCollection
{
}

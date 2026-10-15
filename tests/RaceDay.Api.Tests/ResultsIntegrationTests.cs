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

[Collection("Result tests")]
public sealed class ResultsIntegrationTests
    : IClassFixture<ResultsApplicationFactory>, IAsyncLifetime
{
    private readonly ResultsApplicationFactory factory;

    public ResultsIntegrationTests(ResultsApplicationFactory factory)
    {
        this.factory = factory;
    }

    public async Task InitializeAsync()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
        dbContext.Results.RemoveRange(dbContext.Results);
        dbContext.Enrolments.RemoveRange(dbContext.Enrolments);
        dbContext.Categories.RemoveRange(dbContext.Categories);
        dbContext.Events.RemoveRange(dbContext.Events);
        await dbContext.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task OrganiserCanRecordRetrieveAndUpdateResult()
    {
        var enrolmentId = await CreateEnrolmentAsync();
        using var organiser = await LoginAsync("organiser@example.com");

        var create = await organiser.PostAsJsonAsync(
            $"/api/enrolments/{enrolmentId}/result",
            new { finishTime = "01:42:35", finishingPosition = 47 });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var result = await create.Content.ReadFromJsonAsync<ResultPayload>();
        Assert.NotNull(result);

        using var participant = await LoginAsync("participant@example.com");
        var mine = await participant.GetAsync("/api/results/my");
        Assert.Equal(HttpStatusCode.OK, mine.StatusCode);
        Assert.Contains(result.ResultId.ToString(), await mine.Content.ReadAsStringAsync());

        var update = await organiser.PutAsJsonAsync(
            $"/api/results/{result.ResultId}",
            new { finishTime = "01:41:58", finishingPosition = 46 });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var updated = await update.Content.ReadFromJsonAsync<ResultPayload>();
        Assert.NotNull(updated);
        Assert.Equal(46, updated.FinishingPosition);
    }

    [Fact]
    public async Task AnonymousAndParticipantCannotManageResults()
    {
        var enrolmentId = await CreateEnrolmentAsync();
        using var anonymous = factory.CreateClient();
        using var participant = await LoginAsync("participant@example.com");
        var request = new { finishTime = "00:30:00", finishingPosition = 1 };

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await anonymous.PostAsJsonAsync(
                $"/api/enrolments/{enrolmentId}/result", request)).StatusCode);
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await participant.PostAsJsonAsync(
                $"/api/enrolments/{enrolmentId}/result", request)).StatusCode);
        Assert.Equal(
            HttpStatusCode.OK,
            (await participant.GetAsync("/api/results/my")).StatusCode);
    }

    [Fact]
    public async Task OrganiserCannotManageAnotherOrganisersResult()
    {
        var enrolmentId = await CreateEnrolmentAsync();
        using var owner = await LoginAsync("organiser@example.com");
        var create = await owner.PostAsJsonAsync(
            $"/api/enrolments/{enrolmentId}/result",
            new { finishTime = "00:30:00", finishingPosition = 1 });
        var result = await create.Content.ReadFromJsonAsync<ResultPayload>();
        Assert.NotNull(result);

        using var otherOrganiser = await LoginAsync("second.organiser@example.com");
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await otherOrganiser.PutAsJsonAsync(
                $"/api/results/{result.ResultId}",
                new { finishTime = "00:29:00", finishingPosition = 1 })).StatusCode);
    }

    [Fact]
    public async Task MissingEnrolmentAndResultReturnNotFound()
    {
        using var organiser = await LoginAsync("organiser@example.com");
        var request = new { finishTime = "00:30:00", finishingPosition = 1 };

        Assert.Equal(
            HttpStatusCode.NotFound,
            (await organiser.PostAsJsonAsync(
                "/api/enrolments/99999/result", request)).StatusCode);
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await organiser.PutAsJsonAsync(
                "/api/results/99999", request)).StatusCode);
    }

    [Fact]
    public async Task InvalidResultValuesAreRejected()
    {
        var enrolmentId = await CreateEnrolmentAsync();
        using var organiser = await LoginAsync("organiser@example.com");

        Assert.Equal(
            HttpStatusCode.BadRequest,
            (await organiser.PostAsJsonAsync(
                $"/api/enrolments/{enrolmentId}/result",
                new { finishTime = "00:00:00", finishingPosition = 1 })).StatusCode);
        Assert.Equal(
            HttpStatusCode.BadRequest,
            (await organiser.PostAsJsonAsync(
                $"/api/enrolments/{enrolmentId}/result",
                new { finishTime = "00:30:00", finishingPosition = 0 })).StatusCode);
        Assert.Equal(
            HttpStatusCode.BadRequest,
            (await organiser.PostAsJsonAsync(
                $"/api/enrolments/{enrolmentId}/result",
                new { finishTime = "24:00:00", finishingPosition = 1 })).StatusCode);
    }

    [Fact]
    public async Task DuplicateResultIsRejected()
    {
        var enrolmentId = await CreateEnrolmentAsync();
        using var organiser = await LoginAsync("organiser@example.com");
        var request = new { finishTime = "00:30:00", finishingPosition = 1 };

        Assert.Equal(
            HttpStatusCode.Created,
            (await organiser.PostAsJsonAsync(
                $"/api/enrolments/{enrolmentId}/result", request)).StatusCode);
        Assert.Equal(
            HttpStatusCode.Conflict,
            (await organiser.PostAsJsonAsync(
                $"/api/enrolments/{enrolmentId}/result", request)).StatusCode);
    }

    private async Task<int> CreateEnrolmentAsync()
    {
        using var organiser = await LoginAsync("organiser@example.com");
        var eventResponse = await organiser.PostAsJsonAsync("/api/events", new
        {
            eventTypeId = 1,
            name = $"Result Race {Guid.NewGuid():N}",
            description = "Result integration test event",
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

        using var participant = await LoginAsync("participant@example.com");
        var enrolmentResponse = await participant.PostAsJsonAsync(
            $"/api/events/{raceEvent.EventId}/enrolments",
            new { categoryId = category.CategoryId });
        Assert.Equal(HttpStatusCode.Created, enrolmentResponse.StatusCode);
        var enrolment = await enrolmentResponse.Content.ReadFromJsonAsync<EnrolmentPayload>();
        Assert.NotNull(enrolment);
        return enrolment.EnrolmentId;
    }

    private async Task<HttpClient> LoginAsync(string email)
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
    private sealed record EnrolmentPayload(int EnrolmentId);
    private sealed record ResultPayload(int ResultId, int FinishingPosition);
}

public sealed class ResultsApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string databaseName = $"RaceDayResultTests-{Guid.NewGuid():N}";

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
                TestUserFactory.Create("Organiser", "second.organiser@example.com", 3));
            dbContext.SaveChanges();
        });
    }
}

[CollectionDefinition("Result tests", DisableParallelization = true)]
public sealed class ResultTestsCollection
{
}

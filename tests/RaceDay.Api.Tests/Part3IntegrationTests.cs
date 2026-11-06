using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RaceDay.Api.Data;

namespace RaceDay.Api.Tests;

public sealed class Part3IntegrationTests : IClassFixture<Part3ApplicationFactory>
{
    private static readonly byte[] TinyPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

    private readonly Part3ApplicationFactory factory;

    public Part3IntegrationTests(Part3ApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task OrganiserCanUploadBanner_AndTheImageCanBeReadBack()
    {
        using var organiser = await LoginAsync("organiser@example.com");
        var eventId = await CreateEventAsync(organiser, "Banner Classic");

        using var content = ImageContent("banner.png", "image/png");
        var upload = await organiser.PostAsync($"/api/events/{eventId}/banner", content);
        Assert.Equal(HttpStatusCode.OK, upload.StatusCode);

        var body = await upload.Content.ReadFromJsonAsync<EventBody>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body.BannerImageUrl));
        Assert.StartsWith("/api/media/banners/", body.BannerImageUrl);

        var image = await organiser.GetAsync(body.BannerImageUrl);
        Assert.Equal(HttpStatusCode.OK, image.StatusCode);
        Assert.Equal("image/png", image.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task BannerUploadRejectsMissingImagesAndTheWrongRole()
    {
        using var organiser = await LoginAsync("organiser@example.com");
        var eventId = await CreateEventAsync(organiser, "Guarded Race");

        using var text = new MultipartFormDataContent();
        var notes = new ByteArrayContent("not an image"u8.ToArray());
        notes.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        text.Add(notes, "file", "notes.txt");
        Assert.Equal(
            HttpStatusCode.BadRequest,
            (await organiser.PostAsync($"/api/events/{eventId}/banner", text)).StatusCode);

        using var anonymous = factory.CreateClient();
        using var image = ImageContent("banner.png", "image/png");
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await anonymous.PostAsync($"/api/events/{eventId}/banner", image)).StatusCode);

        using var participant = await LoginAsync("participant@example.com");
        using var participantImage = ImageContent("banner.png", "image/png");
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await participant.PostAsync($"/api/events/{eventId}/banner", participantImage)).StatusCode);
    }

    [Fact]
    public async Task SignedInUserCanUploadAProfilePicture()
    {
        using var participant = await LoginAsync("participant@example.com");
        using var content = ImageContent("profile.png", "image/png");

        var upload = await participant.PostAsync("/api/users/me/picture", content);
        Assert.Equal(HttpStatusCode.OK, upload.StatusCode);

        var profile = await upload.Content.ReadFromJsonAsync<ProfileBody>();
        Assert.NotNull(profile);
        Assert.StartsWith("/api/media/profiles/", profile.ProfileImageUrl);

        var current = await participant.GetFromJsonAsync<ProfileBody>("/api/users/me");
        Assert.Equal(profile.ProfileImageUrl, current?.ProfileImageUrl);
    }

    [Fact]
    public async Task ResultReportsHowManyPeopleFinishedTheEvent()
    {
        using var organiser = await LoginAsync("organiser@example.com");
        var eventId = await CreateEventAsync(organiser, "Field Size Race");
        var categoryId = await CreateCategoryAsync(organiser, eventId, "Open");

        using var first = await LoginAsync("participant@example.com");
        var firstEnrolment = await EnrolAsync(first, eventId, categoryId);

        using var registered = factory.CreateClient();
        var secondEmail = $"second.{Guid.NewGuid():N}@example.com";
        var register = await registered.PostAsJsonAsync("/api/auth/register", new
        {
            firstName = "Second",
            lastName = "Runner",
            email = secondEmail,
            password = "SecurePass1",
            phoneNumber = "0825550101",
            dateOfBirth = "1995-04-12",
            role = "Participant"
        });
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);
        using var second = await LoginAsync(secondEmail);
        var secondEnrolment = await EnrolAsync(second, eventId, categoryId);

        Assert.Equal(HttpStatusCode.Created, (await organiser.PostAsJsonAsync(
            $"/api/enrolments/{firstEnrolment}/result",
            new { finishTime = "01:10:00", finishingPosition = 2 })).StatusCode);
        var secondResult = await organiser.PostAsJsonAsync(
            $"/api/enrolments/{secondEnrolment}/result",
            new { finishTime = "01:05:00", finishingPosition = 1 });
        Assert.Equal(HttpStatusCode.Created, secondResult.StatusCode);
        var recorded = await secondResult.Content.ReadFromJsonAsync<ResultBody>();
        Assert.Equal(2, recorded?.FinisherCount);

        var mine = await second.GetFromJsonAsync<List<ResultBody>>("/api/results/my");
        Assert.Contains(mine!, result => result.FinisherCount == 2 && result.FinishingPosition == 1);
    }

    [Fact]
    public async Task DashboardCountsEnrolmentsForTheSignedInOrganiser()
    {
        using var organiser = await LoginAsync("organiser@example.com");
        var eventId = await CreateEventAsync(organiser, "Dashboard Mile");
        var categoryId = await CreateCategoryAsync(organiser, eventId, "Fun");
        using var participant = await LoginAsync("participant@example.com");
        await EnrolAsync(participant, eventId, categoryId);

        var dashboard = await organiser.GetFromJsonAsync<DashboardBody>("/api/organisers/dashboard");
        Assert.NotNull(dashboard);
        var summary = Assert.Single(dashboard.Events, item => item.EventId == eventId);
        Assert.Equal(1, summary.EnrolmentCount);
        Assert.True(dashboard.UpcomingEventCount >= 1);

        using var participantClient = await LoginAsync("participant@example.com");
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await participantClient.GetAsync("/api/organisers/dashboard")).StatusCode);
    }

    [Fact]
    public async Task PublicEventListFiltersByNameAndUpcoming()
    {
        using var organiser = await LoginAsync("organiser@example.com");
        var token = Guid.NewGuid().ToString("N");
        await CreateEventAsync(organiser, $"Soweto Sunrise {token}");
        await CreateEventAsync(organiser, $"Durban Downhill {token}");

        using var client = factory.CreateClient();
        var named = await client.GetFromJsonAsync<List<EventBody>>(
            $"/api/events?name=Soweto%20Sunrise%20{token}");
        Assert.NotNull(named);
        Assert.Single(named);
        Assert.Contains("Soweto Sunrise", named[0].Name);

        var upcoming = await client.GetFromJsonAsync<List<EventBody>>(
            $"/api/events?name={token}&upcoming=true");
        Assert.NotNull(upcoming);
        Assert.Equal(2, upcoming.Count);
    }

    private async Task<int> CreateEventAsync(HttpClient organiser, string name)
    {
        var response = await organiser.PostAsJsonAsync("/api/events", new
        {
            eventTypeId = 1,
            name,
            description = "Part 3 integration event",
            eventDate = DateTime.UtcNow.AddDays(21).Date,
            location = "Pretoria",
            distanceKm = 5.00m
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<EventBody>();
        Assert.NotNull(created);
        return created.EventId;
    }

    private static async Task<int> CreateCategoryAsync(HttpClient organiser, int eventId, string name)
    {
        var response = await organiser.PostAsJsonAsync(
            $"/api/events/{eventId}/categories",
            new { name = $"{name} {Guid.NewGuid():N}", distanceKm = 5.00m });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<CategoryBody>();
        Assert.NotNull(created);
        return created.CategoryId;
    }

    private static async Task<int> EnrolAsync(HttpClient participant, int eventId, int categoryId)
    {
        var response = await participant.PostAsJsonAsync(
            $"/api/events/{eventId}/enrolments",
            new { categoryId });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<EnrolmentBody>();
        Assert.NotNull(created);
        return created.EnrolmentId;
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

    private static MultipartFormDataContent ImageContent(string fileName, string contentType)
    {
        var content = new MultipartFormDataContent();
        var image = new ByteArrayContent(TinyPng);
        image.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(image, "file", fileName);
        return content;
    }

    private sealed record EventBody(int EventId, string Name, string? BannerImageUrl);
    private sealed record CategoryBody(int CategoryId);
    private sealed record EnrolmentBody(int EnrolmentId);
    private sealed record ProfileBody(string? ProfileImageUrl);
    private sealed record ResultBody(int FinishingPosition, int FinisherCount);
    private sealed record DashboardBody(int UpcomingEventCount, List<DashboardEventBody> Events);
    private sealed record DashboardEventBody(int EventId, int EnrolmentCount);
}

public sealed class Part3ApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string databaseName = $"RaceDayPart3Tests-{Guid.NewGuid():N}";

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

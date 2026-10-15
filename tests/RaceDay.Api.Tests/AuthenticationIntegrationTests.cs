using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Api.Contracts;

namespace RaceDay.Api.Tests;

public sealed class AuthenticationIntegrationTests
    : IClassFixture<AuthenticationApplicationFactory>
{
    private readonly AuthenticationApplicationFactory factory;

    public AuthenticationIntegrationTests(AuthenticationApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task RegisterParticipant_HashesPassword_AndDoesNotGrantOrganiserRole()
    {
        using var client = CreateHttpsClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            firstName = "New",
            lastName = "Participant",
            email = "new.participant@example.com",
            password = "SecurePass1",
            phoneNumber = "0712345678",
            dateOfBirth = "2002-05-14",
            role = "Participant"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var profile = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.NotNull(profile);
        Assert.Equal("Participant", profile.Role);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
        var user = await dbContext.Users.SingleAsync(user => user.Email == "new.participant@example.com");
        Assert.NotEqual("SecurePass1", user.PasswordHash);
        Assert.StartsWith("AQAAAA", user.PasswordHash);
    }

    [Fact]
    public async Task Registration_AllowsOrganiserRoleSelection()
    {
        using var client = CreateHttpsClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            firstName = "New",
            lastName = "Organiser",
            email = "new.organiser@example.com",
            password = "SecurePass1",
            phoneNumber = "0712345678",
            dateOfBirth = "1990-05-14",
            role = "Organiser"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var profile = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.NotNull(profile);
        Assert.Equal("Organiser", profile.Role);
    }

    [Theory]
    [InlineData("weak.password@example.com", "password")]
    [InlineData("not-an-email", "SecurePass1")]
    public async Task Registration_RejectsWeakPasswordOrInvalidEmail(string email, string password)
    {
        using var client = CreateHttpsClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            firstName = "Bad",
            lastName = "Input",
            email,
            password,
            phoneNumber = "0712345678",
            dateOfBirth = "1995-03-10",
            role = "Participant"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Registration_RejectsUnknownRole()
    {
        using var client = CreateHttpsClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            firstName = "Bad",
            lastName = "Role",
            email = "bad.role@example.com",
            password = "SecurePass1",
            phoneNumber = "0712345678",
            dateOfBirth = "1990-05-14",
            role = "Admin"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task LoginProfileAndLogout_UseAndInvalidateServerSession()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var login = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "participant@example.com",
            password = "SecurePass1"
        });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var profile = await client.GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.OK, profile.StatusCode);

        var logout = await client.PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        var afterLogout = await client.GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.Unauthorized, afterLogout.StatusCode);
    }

    [Fact]
    public async Task IncorrectCredentialsAndAnonymousProfile_AreRejected()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var wrongPassword = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "participant@example.com",
            password = "WrongPass1"
        });
        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);

        var anonymousProfile = await client.GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousProfile.StatusCode);
    }

    [Fact]
    public async Task ParticipantCannotUseOrganiserProvisioningEndpoint()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var login = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "participant@example.com",
            password = "SecurePass1"
        });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var response = await client.PostAsJsonAsync("/api/auth/provision-organiser", new
        {
            firstName = "Another",
            lastName = "Organiser",
            email = "another.organiser@example.com",
            password = "SecurePass1",
            phoneNumber = "0712345678",
            dateOfBirth = "1990-05-14"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DuplicateRegistration_ReturnsConflict()
    {
        using var client = CreateHttpsClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            firstName = "Duplicate",
            lastName = "Participant",
            email = "participant@example.com",
            password = "SecurePass1",
            phoneNumber = "0712345678",
            dateOfBirth = "2002-05-14",
            role = "Participant"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task OrganiserCanProvisionAnotherOrganiser()
    {
        using var client = CreateHttpsClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "organiser@example.com",
            password = "SecurePass1"
        });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var response = await client.PostAsJsonAsync("/api/auth/provision-organiser", new
        {
            firstName = "Second",
            lastName = "Organiser",
            email = "second.organiser@example.com",
            password = "SecurePass1",
            phoneNumber = "0712345678",
            dateOfBirth = "1990-05-14"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var profile = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.NotNull(profile);
        Assert.Equal("Organiser", profile.Role);
    }

    [Fact]
    public async Task ProfileUpdateCannotChangeRole()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var login = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "participant@example.com",
            password = "SecurePass1"
        });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var response = await client.PutAsJsonAsync("/api/users/me", new
        {
            firstName = "Updated",
            lastName = "Participant",
            phoneNumber = "0712345678",
            dateOfBirth = "2002-05-14",
            role = "Organiser"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.NotNull(profile);
        Assert.Equal("Participant", profile.Role);
    }

    private HttpClient CreateHttpsClient() =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
}

public sealed class AuthenticationApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string databaseName = $"RaceDayAuthTests-{Guid.NewGuid():N}";

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

            var hasher = new PasswordHasher<User>();
            dbContext.Users.AddRange(
                CreateUser(hasher, "Participant", "participant@example.com", 1),
                CreateUser(hasher, "Organiser", "organiser@example.com", 2));
            dbContext.SaveChanges();
        });
    }

    private static User CreateUser(
        IPasswordHasher<User> hasher,
        string role,
        string email,
        int userId)
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
        user.PasswordHash = hasher.HashPassword(user, "SecurePass1");
        return user;
    }
}

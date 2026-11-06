using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using RaceDay.Api.Auth;
using RaceDay.Api.Data;
using RaceDay.Api.Documentation;
using RaceDay.Api.Models;
using RaceDay.Api.Services;
using AppAuthenticationService = RaceDay.Api.Services.AuthenticationService;
using AppAuthenticationServiceContract = RaceDay.Api.Services.IAuthenticationService;

var builder = WebApplication.CreateBuilder(args);

//Register controller services 
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = "RaceDay.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});
builder.Services
    .AddAuthentication(UserSession.Scheme)
    .AddScheme<AuthenticationSchemeOptions, SessionAuthenticationHandler>(
        UserSession.Scheme,
        _ => { });
builder.Services.AddAuthorization();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<AppAuthenticationServiceContract, AppAuthenticationService>();
builder.Services.Configure<ImageStorageOptions>(
    builder.Configuration.GetSection(ImageStorageOptions.SectionName));
if (string.IsNullOrWhiteSpace(builder.Configuration["AzureStorage:ConnectionString"]))
{
    builder.Services.AddSingleton<IImageStorage, LocalFileImageStorage>();
}
else
{
    builder.Services.AddSingleton<IImageStorage, AzureBlobImageStorage>();
}

// SQL Server is the normal database. InMemory is only for a machine that cannot run LocalDB.
builder.Services.AddDbContext<RaceDayDbContext>(options =>
{
    var provider = builder.Configuration["Database:Provider"];
    if (string.Equals(provider, "InMemory", StringComparison.OrdinalIgnoreCase))
    {
        options.UseInMemoryDatabase(builder.Configuration["Database:Name"] ?? "RaceDayDev");
        return;
    }

    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseSqlServer(connectionString);
});

//Register Swagger / OpenAPI services with metadata and XML documentation 
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RaceDay Event Management API",
        Version = "v1",
        Description = "RESTful Web API for South African community sports events (running, walking, cycling)."
    });
    options.AddSecurityDefinition("SessionCookie", new OpenApiSecurityScheme
    {
        Name = "RaceDay.Session",
        In = ParameterLocation.Cookie,
        Type = SecuritySchemeType.ApiKey,
        Description = "Authenticated server-side session cookie created by POST /api/auth/login."
    });
    // Read the [SwaggerOperation] summaries and descriptions on each action
    options.EnableAnnotations();
    options.OperationFilter<SessionSecurityOperationFilter>();
});

var app = builder.Build();

// Apply any pending migrations in Development so the LocalDB database is created automatically.
// Containers set Database:ApplyMigrations and retry while SQL Server is still starting.
var applyMigrations = app.Environment.IsDevelopment()
    || app.Configuration.GetValue<bool>("Database:ApplyMigrations");
if (applyMigrations)
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();

    if (dbContext.Database.IsRelational())
    {
        var retries = app.Configuration.GetValue<bool>("Database:ApplyMigrations") ? 12 : 1;
        for (var attempt = 1; attempt <= retries; attempt++)
        {
            try
            {
                dbContext.Database.Migrate();
                break;
            }
            catch (Exception) when (attempt < retries)
            {
                Thread.Sleep(TimeSpan.FromSeconds(5));
            }
        }
    }
    else
    {
        dbContext.Database.EnsureCreated();
    }
}

app.UseExceptionHandler();

// Enable Swagger UI in development so the API can be explored without exposing it by default in production.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "RaceDay API v1");
    });
}

app.UseHttpsRedirection();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Make the implicit Program class public for xUnit integration testing if needed 
public partial class Program { }

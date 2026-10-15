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

//Register EF Core DbContext with SQL Server 
builder.Services.AddDbContext<RaceDayDbContext>(options =>
{
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

// Apply any pending migrations in Development so the LocalDB database is created automatically
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();

    // The test projects use the in-memory provider, which does not support migrations
    if (dbContext.Database.IsRelational())
    {
        dbContext.Database.Migrate();
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

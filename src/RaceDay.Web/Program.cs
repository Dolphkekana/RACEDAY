var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = "RaceDay.Web.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});
builder.Services.AddTransient<RaceDay.Web.Api.ApiSessionHandler>();
builder.Services.AddHttpClient<RaceDay.Web.Api.RaceDayApiClient>(client =>
{
    var baseUrl = builder.Configuration["Api:BaseUrl"] ?? "http://localhost:5258";
    client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    // The MVC session stores each user's API cookie. A shared cookie jar would mix those users.
    UseCookies = false
})
.AddHttpMessageHandler<RaceDay.Web.Api.ApiSessionHandler>();

var app = builder.Build();

app.UseExceptionHandler("/Home/Error");

app.UseStaticFiles();
app.UseRouting();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

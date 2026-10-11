using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RaceDay.Api.Data;

namespace RaceDay.Api.Auth;

#pragma warning disable CS0618
public sealed class SessionAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    ISystemClock clock,
    IHttpContextAccessor httpContextAccessor,
    RaceDayDbContext dbContext)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder, clock)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var session = httpContextAccessor.HttpContext?.Session;
        var userId = session?.GetInt32(UserSession.UserIdKey);

        if (userId is null)
        {
            return AuthenticateResult.NoResult();
        }

        var user = await dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.UserId == userId.Value,
                Context.RequestAborted);

        if (user is null)
        {
            session!.Clear();
            return AuthenticateResult.NoResult();
        }

        session!.SetString(UserSession.RoleKey, user.Role);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
            new Claim(ClaimTypes.Role, user.Role)
        };
        var identity = new ClaimsIdentity(claims, UserSession.Scheme);
        return AuthenticateResult.Success(new AuthenticationTicket(
            new ClaimsPrincipal(identity),
            UserSession.Scheme));
    }
}
#pragma warning restore CS0618

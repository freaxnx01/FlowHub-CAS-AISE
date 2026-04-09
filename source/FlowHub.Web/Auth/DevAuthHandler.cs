using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace FlowHub.Web.Auth;

/// <summary>
/// Dev-only authentication handler that auto-signs-in a fixed "Dev Operator"
/// principal so the real auth pipeline (<c>[Authorize]</c>, <c>User.IsInRole</c>,
/// <see cref="Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider"/>)
/// runs identically in dev and prod.
///
/// Per ADR 0001: registered ONLY when <c>builder.Environment.IsDevelopment()</c>.
/// Production goes through OIDC against Authentik (wired in Block 5).
/// Never use <c>[AllowAnonymous]</c> sprinkles to bypass auth in dev.
/// </summary>
public sealed class DevAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Dev";

    public DevAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var rolesCsv = Context.RequestServices
            .GetRequiredService<IConfiguration>()
            .GetValue<string>("Dev:Auth:Roles") ?? "Operator";

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, "Dev Operator"),
            new(ClaimTypes.NameIdentifier, "dev-operator"),
        };

        foreach (var role in rolesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

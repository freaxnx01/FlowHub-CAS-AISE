using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace FlowHub.Web.Auth;

/// <summary>
/// Authentication handler that auto-signs-in a fixed "Demo Operator" principal.
/// Activates automatically when <c>Auth:OIDC:Authority</c> is absent from configuration.
/// Works in any environment (dev, CI, demo), not just Development.
/// Never use <c>[AllowAnonymous]</c> to bypass auth instead of this handler.
/// </summary>
public sealed class DemoAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Demo";

    public DemoAuthHandler(
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
            .GetValue<string>("Demo:Auth:Roles") ?? "Operator";

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, "Demo Operator"),
            new(ClaimTypes.NameIdentifier, "demo-operator"),
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

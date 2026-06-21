using FlowHub.Web.Auth;
using FlowHub.Web.Notifications;
using FlowHub.Web.Pipeline;
using MassTransit;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace FlowHub.Web;

/// <summary>
/// Service-registration helpers extracted from Program.cs so the top-level
/// entry point stays under the CA1502 cyclomatic-complexity gate. Each method
/// preserves the original registration order and behaviour.
/// </summary>
internal static class ProgramRegistration
{
    /// <summary>
    /// Configuration-driven auth: real OIDC when <c>Auth:OIDC:Authority</c> is
    /// set (smart policy scheme dispatches bearer → JWT, browser → cookie+OIDC),
    /// otherwise the demo handler. Auth mode is driven by config, not environment
    /// name (12-Factor III).
    /// </summary>
    public static void AddFlowHubAuthentication(this WebApplicationBuilder builder)
    {
        if (builder.Configuration["Auth:OIDC:Authority"] is { Length: > 0 } oidcAuthority)
        {
            var clientId = builder.Configuration["Auth:OIDC:ClientId"]
                ?? throw new InvalidOperationException("Auth:OIDC:ClientId is required when Auth:OIDC:Authority is set.");
            var clientSecret = builder.Configuration["Auth:OIDC:ClientSecret"]
                ?? throw new InvalidOperationException("Auth:OIDC:ClientSecret is required when Auth:OIDC:Authority is set.");

            // Policy scheme dispatches per request: bearer token → JwtBearer (API clients get 401),
            // anything else → cookie+OIDC (browser flow gets 302 redirect).
            const string SmartScheme = "smart";
            builder.Services
                .AddAuthentication(options =>
                {
                    options.DefaultScheme = SmartScheme;
                    options.DefaultChallengeScheme = SmartScheme;
                })
                .AddPolicyScheme(SmartScheme, SmartScheme, options =>
                {
                    options.ForwardDefaultSelector = ctx =>
                        ctx.Request.Headers.Authorization.ToString()
                            .StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                            ? JwtBearerDefaults.AuthenticationScheme
                            : Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme;
                })
                .AddCookie()
                .AddJwtBearer(options =>
                {
                    options.Authority = oidcAuthority;
                    options.Audience = clientId;
                    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
                })
                .AddOpenIdConnect(options =>
                {
                    options.Authority = oidcAuthority;
                    options.ClientId = clientId;
                    options.ClientSecret = clientSecret;
                    options.ResponseType = "code";
                    options.SaveTokens = true;
                });
        }
        else
        {
            builder.Services
                .AddAuthentication(DemoAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, DemoAuthHandler>(DemoAuthHandler.SchemeName, _ => { });
        }
    }

    /// <summary>
    /// Demo-only operator notifications → ntfy.sh. Dormant unless
    /// <c>Demo:Notify:Ntfy:BaseUrl</c> + Topic are set. Returns whether the demo
    /// notifier was registered, so messaging can wire its consumer to match.
    /// </summary>
    public static bool AddFlowHubDemoNotifications(this WebApplicationBuilder builder)
    {
        var demoNotify = builder.Configuration.GetSection(DemoNotifyOptions.SectionName).Get<DemoNotifyOptions>()
            ?? new DemoNotifyOptions();
        if (!demoNotify.IsConfigured)
        {
            return false;
        }

        builder.Services.Configure<DemoNotifyOptions>(builder.Configuration.GetSection(DemoNotifyOptions.SectionName));
        builder.Services.AddHttpClient<ICaptureNotifier, NtfyCaptureNotifier>(client =>
        {
            client.BaseAddress = new Uri(demoNotify.BaseUrl!.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(10);
        });
        return true;
    }

    /// <summary>
    /// Block 3 Slice B — MassTransit pipeline (consumers + retry policies +
    /// transport selection). The demo notification consumer is added only when
    /// <paramref name="demoNotifyEnabled"/>.
    /// </summary>
    public static void AddFlowHubMessaging(this WebApplicationBuilder builder, bool demoNotifyEnabled)
    {
        builder.Services.AddMassTransit(x =>
        {
            x.SetKebabCaseEndpointNameFormatter();

            x.AddConsumer<CaptureEnrichmentConsumer>(c =>
                c.UseMessageRetry(r => r.Intervals(100, 500)));

            x.AddConsumer<CaptureEmbeddingConsumer>(c =>
                c.UseMessageRetry(r => r.Intervals(500, 2000, 5000)));

            x.AddConsumer<SkillRoutingConsumer>(c =>
                c.UseMessageRetry(r => r.Intervals(500, 2000, 5000)));

            // No retry policy — fault observer is best-effort per spec D5
            // (recursive retry on Fault<T> would loop forever).
            x.AddConsumer<LifecycleFaultObserver>();

            // Demo-only: announce new captures to ntfy.sh (registered only when configured).
            if (demoNotifyEnabled)
            {
                x.AddConsumer<CaptureNotificationConsumer>(c =>
                    c.UseMessageRetry(r => r.Intervals(500, 2000)));
            }

            if (string.Equals(builder.Configuration["Bus:Transport"], "RabbitMq", StringComparison.OrdinalIgnoreCase))
            {
                x.UsingRabbitMq((ctx, cfg) =>
                {
                    cfg.Host(builder.Configuration["Bus:RabbitMq:Host"], "/", h =>
                    {
                        h.Username(builder.Configuration["Bus:RabbitMq:Username"] ?? "guest");
                        h.Password(builder.Configuration["Bus:RabbitMq:Password"] ?? "guest");
                    });
                    cfg.ConfigureEndpoints(ctx);
                });
            }
            else
            {
                x.UsingInMemory((ctx, cfg) => cfg.ConfigureEndpoints(ctx));
            }
        });
    }
}

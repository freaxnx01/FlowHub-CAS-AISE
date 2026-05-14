using FlowHub.AI;
using FlowHub.Api;
using FlowHub.Api.Endpoints;
using FlowHub.Core.Classification;
using FlowHub.Core.Health;
using FlowHub.Core.Skills;
using FlowHub.Persistence;
using FlowHub.Skills;
using FlowHub.Web.Auth;
using FlowHub.Web.Components;
using FlowHub.Web.Pipeline;
using FlowHub.Web.Testing;
using MassTransit;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using MudBlazor.Services;
using OpenTelemetry.Metrics;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Razor components + Blazor Server interactivity (per ADR 0001).
builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

// MudBlazor — only component library per CLAUDE.md.
builder.Services.AddMudServices();

// Auth mode is driven by configuration, not environment name (12-Factor III).
// Set Auth__OIDC__Authority + Auth__OIDC__ClientId + Auth__OIDC__ClientSecret for real OIDC.
// Omit all Auth__OIDC__* vars to activate DemoAuthHandler (any environment).
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

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

// Health checks — /health/live exposes liveness for the Docker healthcheck (see docker-compose.yml).
builder.Services.AddHealthChecks();

// Prometheus metrics endpoint — scraped by docker/prometheus/prometheus.yml at /metrics.
builder.Services.AddOpenTelemetry()
    .WithMetrics(m => m
        .AddAspNetCoreInstrumentation()
        .AddRuntimeInstrumentation()
        .AddPrometheusExporter());

// Block 4 prep (Beta MVP) — EF Core SQLite persistence.
// `AddFlowHubPersistence` registers FlowHubDbContext (scoped) + EfCaptureService as ICaptureService.
// Migrations apply at startup via the MigrationRunner IHostedService.
builder.Services.AddFlowHubPersistence(builder.Configuration);

// Block 3 Slice C — AI-backed classifier (per ADR 0004) with keyword fallback.
// Uses real provider when Ai:Provider + Ai:<P>:ApiKey are set; silently falls back
// to the deterministic KeywordClassifier otherwise so `make run` works zero-config.
builder.Services.AddFlowHubAi(builder.Configuration);
builder.Services.AddFlowHubEmbeddings(builder.Configuration);

// Beta MVP — real skill integrations behind ISkillIntegration. AddFlowHubSkills mirrors
// AddFlowHubAi: silent fallback if Skills:<X>:BaseUrl or :ApiToken is missing.
builder.Services.AddFlowHubSkills(builder.Configuration);

// Block 3 Slice B — MassTransit pipeline.
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

// Block 3 Slice A — REST API surface for non-UI consumers.
builder.Services.AddFlowHubApi();

// E2E-only: fault-injection decorators (no-op unless FLOWHUB_E2E_FAULTS_ENABLED=true).
// Used by the J26 / J28 Playwright specs to force ISkillRegistry / IIntegrationHealthService
// to throw — bUnit owns the same negative path at the component level.
if (E2EFaultExtensions.IsEnabled)
{
    builder.Services.AddE2EFaultInjection();
}

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapFlowHubApi();
app.MapOpenApi("/openapi/v1.json");
app.MapScalarApiReference();

// E2E-only test endpoints: POST /test/faults/{skills|integrations}/{arm|disarm}.
if (E2EFaultExtensions.IsEnabled)
{
    app.MapE2EFaultEndpoints();
}

// Liveness — anonymous so the Docker healthcheck (and OIDC mode) doesn't get a 302/401.
app.MapHealthChecks("/health/live").AllowAnonymous();

// Prometheus scrape endpoint — anonymous for in-network scrapes.
app.MapPrometheusScrapingEndpoint().AllowAnonymous();

app.Run();

// Expose Program for WebApplicationFactory<Program> in integration tests.
public partial class Program { }

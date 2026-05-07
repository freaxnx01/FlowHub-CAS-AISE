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
using MassTransit;
using Microsoft.AspNetCore.Authentication;
using MudBlazor.Services;
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
    builder.Services
        .AddAuthentication(options =>
        {
            options.DefaultScheme = Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectDefaults.AuthenticationScheme;
        })
        .AddCookie()
        .AddOpenIdConnect(options =>
        {
            options.Authority = oidcAuthority;
            options.ClientId = builder.Configuration["Auth:OIDC:ClientId"]
                ?? throw new InvalidOperationException("Auth:OIDC:ClientId is required when Auth:OIDC:Authority is set.");
            options.ClientSecret = builder.Configuration["Auth:OIDC:ClientSecret"]
                ?? throw new InvalidOperationException("Auth:OIDC:ClientSecret is required when Auth:OIDC:Authority is set.");
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

// Block 4 prep (Beta MVP) — EF Core SQLite persistence.
// `AddFlowHubPersistence` registers FlowHubDbContext (scoped) + EfCaptureService as ICaptureService.
// Migrations apply at startup via the MigrationRunner IHostedService.
builder.Services.AddFlowHubPersistence(builder.Configuration);

// Block 3 Slice C — AI-backed classifier (per ADR 0004) with keyword fallback.
// Uses real provider when Ai:Provider + Ai:<P>:ApiKey are set; silently falls back
// to the deterministic KeywordClassifier otherwise so `make run` works zero-config.
builder.Services.AddFlowHubAi(builder.Configuration);

// Beta MVP — real skill integrations behind ISkillIntegration. AddFlowHubSkills mirrors
// AddFlowHubAi: silent fallback if Skills:<X>:BaseUrl or :ApiToken is missing.
builder.Services.AddFlowHubSkills(builder.Configuration);

// Block 3 Slice B — MassTransit pipeline.
builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();

    x.AddConsumer<CaptureEnrichmentConsumer>(c =>
        c.UseMessageRetry(r => r.Intervals(100, 500)));

    x.AddConsumer<SkillRoutingConsumer>(c =>
        c.UseMessageRetry(r => r.Intervals(500, 2000, 5000)));

    // No retry policy — fault observer is best-effort per spec D5
    // (recursive retry on Fault<T> would loop forever).
    x.AddConsumer<LifecycleFaultObserver>();

    if (string.Equals(builder.Configuration["Bus:Transport"], "RabbitMq", StringComparison.OrdinalIgnoreCase))
    {
        x.UsingRabbitMq((ctx, cfg) =>
        {
            cfg.Host(builder.Configuration["Bus:RabbitMq:Host"]);
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

app.Run();

// Expose Program for WebApplicationFactory<Program> in integration tests.
public partial class Program { }

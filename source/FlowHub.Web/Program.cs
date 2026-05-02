using FlowHub.Api;
using FlowHub.Api.Endpoints;
using FlowHub.Core.Captures;
using FlowHub.Core.Classification;
using FlowHub.Core.Health;
using FlowHub.Core.Skills;
using FlowHub.Skills;
using FlowHub.Web.Auth;
using FlowHub.Web.Components;
using FlowHub.Web.Pipeline;
using FlowHub.Web.Stubs;
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

// Authentication.
// Dev: DevAuthHandler auto-signs-in 'Dev Operator' so the real auth pipeline runs.
// Prod: OIDC against Authentik — wired in Block 5 (Deployment).
if (builder.Environment.IsDevelopment())
{
    builder.Services
        .AddAuthentication(DevAuthHandler.SchemeName)
        .AddScheme<AuthenticationSchemeOptions, DevAuthHandler>(DevAuthHandler.SchemeName, _ => { });
}
else
{
    // TODO Block 5 — wire OpenIdConnect against Authentik via env vars (Auth__OIDC__*).
    builder.Services.AddAuthentication();
}

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

// Block 2 stub services — replaced incrementally as later blocks land.
// CaptureServiceStub now publishes CaptureCreated on submit, so it must be
// constructed via factory using IBus (singleton publish endpoint) — plain
// AddSingleton<ICaptureService, CaptureServiceStub>() would fail because
// IPublishEndpoint is registered Scoped by MassTransit and a Singleton can't
// capture a Scoped dependency.
// TODO Block 4: when the EF Core impl lands, ICaptureService becomes Scoped
// (DbContext is Scoped) and can take IPublishEndpoint directly — drop the factory.
builder.Services.AddSingleton<ICaptureService>(sp =>
    new CaptureServiceStub(sp.GetRequiredService<IBus>()));
builder.Services.AddSingleton<ISkillRegistry, SkillRegistryStub>();
builder.Services.AddSingleton<IIntegrationHealthService, IntegrationHealthServiceStub>();

// Block 3 Slice B — classifier + skill integrations.
builder.Services.AddSingleton<IClassifier, KeywordClassifier>();
builder.Services.AddSingleton<ISkillIntegration>(sp =>
    new LoggingSkillIntegration("Wallabag", sp.GetRequiredService<ILogger<LoggingSkillIntegration>>()));
builder.Services.AddSingleton<ISkillIntegration>(sp =>
    new LoggingSkillIntegration("Vikunja", sp.GetRequiredService<ILogger<LoggingSkillIntegration>>()));

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

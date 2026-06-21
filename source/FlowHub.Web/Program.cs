using FlowHub.AI;
using FlowHub.Api;
using FlowHub.Api.Endpoints;
using FlowHub.Persistence;
using FlowHub.Skills;
using FlowHub.Web;
using FlowHub.Web.Components;
using FlowHub.Web.Testing;
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

// Configuration-driven auth (real OIDC vs demo handler) — see ProgramRegistration.
builder.AddFlowHubAuthentication();
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

// Capture file uploads: bind options, register storage + policy.
builder.Services
    .AddOptions<FlowHub.Core.Captures.UploadOptions>()
    .Bind(builder.Configuration.GetSection("FlowHub:Uploads"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton<FlowHub.Core.Captures.IAttachmentStorage, FlowHub.Persistence.FilesystemAttachmentStorage>();
builder.Services.AddSingleton<FlowHub.Core.Captures.IUploadPolicy, FlowHub.Web.Uploads.UploadPolicy>();

// Block 3 Slice C — AI-backed classifier (per ADR 0004) with keyword fallback.
// Uses real provider when Ai:Provider + Ai:<P>:ApiKey are set; silently falls back
// to the deterministic KeywordClassifier otherwise so `make run` works zero-config.
builder.Services.AddFlowHubAi(builder.Configuration);
builder.Services.AddFlowHubEmbeddings(builder.Configuration);

// Beta MVP — real skill integrations behind ISkillIntegration. AddFlowHubSkills mirrors
// AddFlowHubAi: silent fallback if Skills:<X>:BaseUrl or :ApiToken is missing.
builder.Services.AddFlowHubSkills(builder.Configuration);

// Classification trace panel — always registered (defaults to Enabled=false) so the capture
// detail page can always resolve IOptions<DemoTraceOptions>; set Demo:Trace:Enabled=true on the
// public demo to surface the reasoning panel transparently.
builder.Services.Configure<FlowHub.Web.Demo.DemoTraceOptions>(
    builder.Configuration.GetSection(FlowHub.Web.Demo.DemoTraceOptions.SectionName));

// Demo-only operator notifications → ntfy.sh (dormant unless configured) — see ProgramRegistration.
var demoNotifyEnabled = builder.AddFlowHubDemoNotifications();

// Block 3 Slice B — MassTransit pipeline (consumers, retries, transport) — see ProgramRegistration.
builder.AddFlowHubMessaging(demoNotifyEnabled);

// Block 3 Slice A — REST API surface for non-UI consumers.
builder.Services.AddFlowHubApi();

// E2E-only: fault-injection decorators (no-op unless FLOWHUB_E2E_FAULTS_ENABLED=true).
// Used by the J26 / J28 Playwright specs to force ISkillRegistry / IIntegrationHealthService
// to throw — bUnit owns the same negative path at the component level.
if (E2EFaultExtensions.IsEnabled)
{
    builder.Services.AddE2EFaultInjection();
}

var maxUpload = builder.Configuration.GetValue<long?>("FlowHub:Uploads:MaxBytes")
    ?? FlowHub.Core.Captures.UploadOptions.DefaultMaxBytes;

builder.Services.Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(o =>
    o.Limits.MaxRequestBodySize = maxUpload);
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(o =>
    o.MultipartBodyLengthLimit = maxUpload);

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

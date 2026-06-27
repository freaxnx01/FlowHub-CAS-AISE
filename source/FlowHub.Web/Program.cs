using FlowHub.AI;
using FlowHub.Api;
using FlowHub.Api.Endpoints;
using FlowHub.Core.Telemetry;
using FlowHub.Persistence;
using FlowHub.Skills;
using FlowHub.Web;
using FlowHub.Web.Components;
using FlowHub.Web.Observability;
using FlowHub.Web.Testing;
using MudBlazor.Services;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
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
builder.Services.AddAuthorization(options =>
{
    // Defence-in-depth: admin APIs require an explicit Admin role on top of
    // authentication. The demo operator principal (DemoAuthHandler) is granted
    // only "Operator" by default, so /api/v1/admin/* is inaccessible in the
    // public demo unless Demo:Auth:Roles is widened deliberately. See issue #100.
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
});
builder.Services.AddCascadingAuthenticationState();

// Health checks — /health/live exposes liveness for the Docker healthcheck (see docker-compose.yml).
builder.Services.AddHealthChecks();

// OpenTelemetry — metrics scraped at /metrics, traces exported via OTLP (if Otlp__Endpoint set)
// and always to Console for in-build/demo visibility. PII policy enforced by
// `TagAllowListProcessor` (ADR 0009 §1/§2/§4) as defense-in-depth.
var otlpEndpoint = builder.Configuration["Otlp:Endpoint"];
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService(serviceName: "flowhub.web"))
    .WithMetrics(m => m
        .AddAspNetCoreInstrumentation()
        .AddRuntimeInstrumentation()
        .AddPrometheusExporter())
    .WithTracing(t =>
    {
        t.AddSource(FlowHubActivityTags.Source)
         .AddSource("MassTransit")
         .AddSource("Experimental.Microsoft.Extensions.AI")
         .AddAspNetCoreInstrumentation(o => o.RecordException = true)
         .AddHttpClientInstrumentation(o => o.RecordException = true)
         .AddEntityFrameworkCoreInstrumentation(o => o.SetDbStatementForText = false)
         .AddProcessor(new TagAllowListProcessor())
         .AddConsoleExporter();

        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            t.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
        }
    });

// `AddFlowHubPersistence` registers FlowHubDbContext (scoped) + EfCaptureService as ICaptureService.
// Migrations apply *before* startup via the `flowhub.migrations` Compose container
// (see docker/migrations/Dockerfile — runs a `dotnet ef migrations bundle` binary);
// the web container depends on it via `service_completed_successfully` (12-Factor XII).
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
// to throw — bUnit owns the same negative path at the component level. The IsEnabled
// guard lives inside the wrapper so this file stays env-var-free.
builder.Services.AddE2EFaultInjectionIfEnabled();

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
// Wrapper hides the IsEnabled guard so Program.cs doesn't reference env-vars directly.
app.MapE2EFaultEndpointsIfEnabled();

// Liveness — anonymous so the Docker healthcheck (and OIDC mode) doesn't get a 302/401.
app.MapHealthChecks("/health/live").AllowAnonymous();

// Prometheus scrape endpoint — anonymous for in-network scrapes.
app.MapPrometheusScrapingEndpoint().AllowAnonymous();

app.Run();

// Expose Program for WebApplicationFactory<Program> in integration tests.
public partial class Program { }

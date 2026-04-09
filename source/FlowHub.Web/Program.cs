using FlowHub.Core.Captures;
using FlowHub.Core.Health;
using FlowHub.Web.Auth;
using FlowHub.Web.Components;
using FlowHub.Web.Stubs;
using Microsoft.AspNetCore.Authentication;
using MudBlazor.Services;

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

// Block 2 stub services — replaced with real implementations in Block 3.
builder.Services.AddSingleton<ICaptureService, CaptureServiceStub>();
builder.Services.AddSingleton<ISkillRegistry, SkillRegistryStub>();
builder.Services.AddSingleton<IIntegrationHealthService, IntegrationHealthServiceStub>();

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

app.Run();

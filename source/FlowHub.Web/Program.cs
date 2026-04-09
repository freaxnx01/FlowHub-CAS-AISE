using FlowHub.Core.Captures;
using FlowHub.Core.Health;
using FlowHub.Web.Components;
using FlowHub.Web.Stubs;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Razor components + Blazor Server interactivity (per ADR 0001).
builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

// MudBlazor — only component library per CLAUDE.md.
builder.Services.AddMudServices();

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

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

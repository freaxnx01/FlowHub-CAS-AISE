using FlowHub.Web.Components.Pages;
using FlowHub.Web.Stubs;
using CapturesPage = FlowHub.Web.Components.Pages.Captures;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;

namespace FlowHub.Web.ComponentTests;

/// <summary>
/// End-to-end smoke tests using real Bogus stubs (not mocks).
/// Automates the manual walkthrough: "Happy path through all pages +
/// at least 1 orphan + 1 unhandled drill-down."
/// Replaces the vault TODO item for manual pre-PVA verification.
/// </summary>
public class SmokeTests : TestContext
{
    private readonly CaptureServiceStub _captureService = new(Substitute.For<IPublishEndpoint>());
    private readonly SkillRegistryStub _skillRegistry = new();
    private readonly IntegrationHealthServiceStub _integrationHealthService = new();

    public SmokeTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<ICaptureService>(_captureService);
        Services.AddSingleton<ISkillRegistry>(_skillRegistry);
        Services.AddSingleton<IIntegrationHealthService>(_integrationHealthService);
        RenderComponent<MudPopoverProvider>();
    }

    // ──────────────────────────────────────────────────────────
    // Dashboard
    // ──────────────────────────────────────────────────────────

    [Fact]
    public void Dashboard_RendersAllFourCards_WithBogusData()
    {
        var cut = RenderComponent<Dashboard>();

        cut.Markup.Should().Contain("Needs attention");
        cut.Markup.Should().Contain("Recent captures");
        cut.Markup.Should().Contain("Skill health");
        cut.Markup.Should().Contain("Integration health");
    }

    [Fact]
    public void Dashboard_NeedsAttention_ShowsOrphanAndUnhandledCounts()
    {
        var cut = RenderComponent<Dashboard>();

        // Bogus stub seeds 2 orphan + 1 unhandled
        cut.Markup.Should().Contain("orphan");
        cut.Markup.Should().Contain("unhandled");
    }

    [Fact]
    public void Dashboard_RecentCaptures_ShowsCaptureContent()
    {
        var cut = RenderComponent<Dashboard>();

        // Verify at least one seeded capture content appears
        cut.Markup.Should().Contain("Inception");
    }

    [Fact]
    public void Dashboard_SkillHealth_ShowsSkillNames()
    {
        var cut = RenderComponent<Dashboard>();

        cut.Markup.Should().Contain("Books");
        cut.Markup.Should().Contain("Movies");
        cut.Markup.Should().Contain("Quotes");
    }

    [Fact]
    public void Dashboard_IntegrationHealth_ShowsIntegrationNames()
    {
        var cut = RenderComponent<Dashboard>();

        cut.Markup.Should().Contain("Wallabag");
        cut.Markup.Should().Contain("Vikunja");
        cut.Markup.Should().Contain("Obsidian");
    }

    // ──────────────────────────────────────────────────────────
    // Captures list
    // ──────────────────────────────────────────────────────────

    [Fact]
    public void CapturesList_ShowsAllTwelveCaptures()
    {
        var cut = RenderComponent<CapturesPage>();

        cut.Markup.Should().Contain("Results: 12");
    }

    [Fact]
    public void CapturesList_ContainsOrphanAndUnhandledBadges()
    {
        var cut = RenderComponent<CapturesPage>();

        cut.Markup.Should().Contain("orphan");
        cut.Markup.Should().Contain("unhandled");
        cut.Markup.Should().Contain("completed");
    }

    // ──────────────────────────────────────────────────────────
    // Capture detail — orphan drill-down
    // ──────────────────────────────────────────────────────────

    [Fact]
    public async Task CaptureDetail_Orphan_ShowsFailureReasonAndActions()
    {
        var all = await _captureService.GetAllAsync();
        var orphan = all.First(c => c.Stage == LifecycleStage.Orphan);

        var cut = RenderComponent<CaptureDetail>(p => p.Add(c => c.Id, orphan.Id));

        // Failure alert with reason
        cut.Markup.Should().Contain("Routing failed");
        cut.Markup.Should().Contain(orphan.FailureReason!);

        // Full content displayed
        cut.Markup.Should().Contain(orphan.Content);

        // Action buttons present
        cut.Markup.Should().Contain("Retry routing");
        cut.Markup.Should().Contain("Reassign skill");
        cut.Markup.Should().Contain("Ignore");
    }

    // ──────────────────────────────────────────────────────────
    // Capture detail — unhandled drill-down
    // ──────────────────────────────────────────────────────────

    [Fact]
    public async Task CaptureDetail_Unhandled_ShowsNoSkillMessageAndAssignAction()
    {
        var all = await _captureService.GetAllAsync();
        var unhandled = all.First(c => c.Stage == LifecycleStage.Unhandled);

        var cut = RenderComponent<CaptureDetail>(p => p.Add(c => c.Id, unhandled.Id));

        // Info alert
        cut.Markup.Should().Contain("No Skill matched this Capture");

        // Full content
        cut.Markup.Should().Contain(unhandled.Content);

        // Assign action (not Retry — unhandled has no Skill to retry)
        cut.Markup.Should().Contain("Assign skill");
        cut.Markup.Should().Contain("Ignore");
        cut.Markup.Should().NotContain("Retry routing");
    }

    // ──────────────────────────────────────────────────────────
    // Capture detail — completed (no actions)
    // ──────────────────────────────────────────────────────────

    [Fact]
    public async Task CaptureDetail_Completed_ShowsContentButNoActions()
    {
        var all = await _captureService.GetAllAsync();
        var completed = all.First(c => c.Stage == LifecycleStage.Completed);

        var cut = RenderComponent<CaptureDetail>(p => p.Add(c => c.Id, completed.Id));

        // Content + metadata present
        cut.Markup.Should().Contain(completed.Content);
        cut.Markup.Should().Contain("Metadata");

        // No action buttons, no failure alert
        cut.Markup.Should().NotContain("Retry routing");
        cut.Markup.Should().NotContain("Reassign skill");
        cut.Markup.Should().NotContain("Assign skill");
        cut.Markup.Should().NotContain("Routing failed");
    }

    // ──────────────────────────────────────────────────────────
    // Capture detail — not found
    // ──────────────────────────────────────────────────────────

    [Fact]
    public void CaptureDetail_InvalidId_ShowsNotFoundAlert()
    {
        var cut = RenderComponent<CaptureDetail>(p => p.Add(c => c.Id, Guid.Empty));

        cut.Markup.Should().Contain("Capture not found");
    }

    // ──────────────────────────────────────────────────────────
    // New Capture
    // ──────────────────────────────────────────────────────────

    [Fact]
    public void NewCapture_RendersFormWithSkillDropdown()
    {
        var cut = RenderComponent<NewCapture>();

        cut.Markup.Should().Contain("Content");
        cut.Markup.Should().Contain("Skill override");
        cut.Markup.Should().Contain("Let AI decide");
        cut.Markup.Should().Contain("Submit");
    }

    // ──────────────────────────────────────────────────────────
    // Skills page
    // ──────────────────────────────────────────────────────────

    [Fact]
    public void SkillsPage_ShowsAllSkillsFromRegistry()
    {
        var cut = RenderComponent<FlowHub.Web.Components.Pages.Skills>();

        cut.Markup.Should().Contain("Books");
        cut.Markup.Should().Contain("Movies");
        cut.Markup.Should().Contain("Articles");
        cut.Markup.Should().Contain("Quotes");
        cut.Markup.Should().Contain("Knowledge");
        cut.Markup.Should().Contain("Belege");
    }

    // ──────────────────────────────────────────────────────────
    // Integrations page
    // ──────────────────────────────────────────────────────────

    [Fact]
    public void IntegrationsPage_ShowsAllIntegrationsFromService()
    {
        var cut = RenderComponent<FlowHub.Web.Components.Pages.Integrations>();

        cut.Markup.Should().Contain("Wallabag");
        cut.Markup.Should().Contain("Wekan");
        cut.Markup.Should().Contain("Vikunja");
        cut.Markup.Should().Contain("Paperless");
        cut.Markup.Should().Contain("Obsidian");
        cut.Markup.Should().Contain("Authentik");
    }
}

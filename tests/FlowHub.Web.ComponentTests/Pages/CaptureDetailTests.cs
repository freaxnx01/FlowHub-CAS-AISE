using FlowHub.Web.Components.Pages;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;

namespace FlowHub.Web.ComponentTests.Pages;

public class CaptureDetailTests : TestContext
{
    private readonly ICaptureService _captureService = Substitute.For<ICaptureService>();
    private readonly ISkillRegistry _skillRegistry = Substitute.For<ISkillRegistry>();

    public CaptureDetailTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton(_captureService);
        Services.AddSingleton(_skillRegistry);
        RenderComponent<MudPopoverProvider>();

        _skillRegistry.GetHealthAsync(Arg.Any<CancellationToken>())
            .Returns(new SkillHealth[] { new("Books", HealthStatus.Healthy, 10) });
    }

    [Fact]
    public void Render_UnknownId_ShowsNotFoundAlert()
    {
        _captureService.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Capture?)null);

        var cut = RenderComponent<CaptureDetail>(p => p.Add(c => c.Id, Guid.NewGuid()));

        cut.Markup.Should().Contain("Capture not found");
    }

    [Fact]
    public void Render_Completed_ShowsContentWithoutFailureAlertOrActions()
    {
        var id = Guid.NewGuid();
        _captureService.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(new Capture(id, ChannelKind.Web, "Done capture body", DateTimeOffset.UtcNow,
                LifecycleStage.Completed, "Books", null, "Some title"));

        var cut = RenderComponent<CaptureDetail>(p => p.Add(c => c.Id, id));

        cut.Markup.Should().Contain("Done capture body");
        cut.Markup.Should().Contain("Some title");
        cut.Markup.Should().NotContain("Routing failed");
        cut.Markup.Should().NotContain("Retry routing");
    }

    [Fact]
    public void Render_WithEnrichment_ShowsEnrichmentBlock()
    {
        var id = Guid.NewGuid();
        _captureService.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(new Capture(id, ChannelKind.Web, "\"...\" — Marcus Aurelius", DateTimeOffset.UtcNow,
                LifecycleStage.Unhandled, "Vikunja", FailureReason: "no integration registered",
                Title: "Quote — Marcus Aurelius",
                EnrichmentDescription: "\"You have power over your mind.\" — Marcus Aurelius\n\nAbout Marcus Aurelius: Roman emperor and Stoic philosopher."));

        var cut = RenderComponent<CaptureDetail>(p => p.Add(c => c.Id, id));

        cut.Markup.Should().Contain("Enrichment");
        cut.Markup.Should().Contain("About Marcus Aurelius");
    }

    [Fact]
    public void Render_Orphan_ShowsRoutingFailedAlertAndRetryButton()
    {
        var id = Guid.NewGuid();
        _captureService.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(new Capture(id, ChannelKind.Web, "stuck capture", DateTimeOffset.UtcNow,
                LifecycleStage.Orphan, null, "no skill matched"));

        var cut = RenderComponent<CaptureDetail>(p => p.Add(c => c.Id, id));

        cut.Markup.Should().Contain("Routing failed");
        cut.Markup.Should().Contain("no skill matched");
        cut.Markup.Should().Contain("Retry routing");
        cut.Markup.Should().Contain("Reassign skill");
        cut.Markup.Should().Contain("Ignore");
    }

    [Fact]
    public void Render_Unhandled_ShowsIntegrationFailedAlertAndAssignButton()
    {
        var id = Guid.NewGuid();
        _captureService.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(new Capture(id, ChannelKind.Web, "blocked capture", DateTimeOffset.UtcNow,
                LifecycleStage.Unhandled, "Books", "wallabag 503"));

        var cut = RenderComponent<CaptureDetail>(p => p.Add(c => c.Id, id));

        cut.Markup.Should().Contain("Skill integration failed");
        cut.Markup.Should().Contain("wallabag 503");
        cut.Markup.Should().Contain("Assign skill");
        cut.Markup.Should().NotContain("Retry routing");
    }

    [Fact]
    public void Render_LoadFailure_ShowsErrorAlertWithRetry()
    {
        _captureService.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<Capture?>(new InvalidOperationException("db offline")));

        var cut = RenderComponent<CaptureDetail>(p => p.Add(c => c.Id, Guid.NewGuid()));

        cut.Markup.Should().Contain("Could not load capture");
        cut.Markup.Should().Contain("db offline");
    }
}

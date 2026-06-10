using FlowHub.Core.Classification;
using FlowHub.Web.Components.Pages;
using FlowHub.Web.Demo;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MudBlazor;
using MudBlazor.Services;

namespace FlowHub.Web.ComponentTests.Pages;

public class CaptureDetailTests : TestContext
{
    private readonly ICaptureService _captureService = Substitute.For<ICaptureService>();
    private readonly ISkillRegistry _skillRegistry = Substitute.For<ISkillRegistry>();
    private readonly IClassificationCostEstimator _costEstimator = Substitute.For<IClassificationCostEstimator>();

    public CaptureDetailTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton(_captureService);
        Services.AddSingleton(_skillRegistry);
        Services.AddSingleton(_costEstimator);
        Services.AddSingleton(Options.Create(new DemoTraceOptions()));
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

    [Fact]
    public void Render_TraceGateOn_ShowsClassificationTracePanel()
    {
        using var ctx = new TestContext();
        ctx.Services.AddMudServices();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var captureService = Substitute.For<ICaptureService>();
        var skillRegistry = Substitute.For<ISkillRegistry>();
        var costEstimator = Substitute.For<IClassificationCostEstimator>();

        ctx.Services.AddSingleton(captureService);
        ctx.Services.AddSingleton(skillRegistry);
        ctx.Services.AddSingleton(costEstimator);
        ctx.Services.AddSingleton(Options.Create(new DemoTraceOptions { Enabled = true }));
        ctx.RenderComponent<MudPopoverProvider>();

        skillRegistry.GetHealthAsync(Arg.Any<CancellationToken>())
            .Returns(new SkillHealth[] { new("Books", HealthStatus.Healthy, 10) });

        var id = Guid.NewGuid();
        var trace = new ClassifierTrace(ClassifierKind.Ai, 900, "OpenRouter", "gemma:free", 80, 12);
        captureService.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(new Capture(id, ChannelKind.Web, "AI classified capture", DateTimeOffset.UtcNow,
                LifecycleStage.Classified, "Books", ClassifierTrace: trace));

        costEstimator.Estimate(Arg.Any<string?>(), Arg.Any<int?>(), Arg.Any<int?>()).Returns((decimal?)0m);

        var cut = ctx.RenderComponent<CaptureDetail>(p => p.Add(c => c.Id, id));

        cut.Markup.Should().Contain("Classification trace");
        cut.Markup.Should().Contain("OpenRouter");
    }

    [Fact]
    public void Render_TraceGateOff_HidesClassificationTracePanel()
    {
        using var ctx = new TestContext();
        ctx.Services.AddMudServices();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var captureService = Substitute.For<ICaptureService>();
        var skillRegistry = Substitute.For<ISkillRegistry>();
        var costEstimator = Substitute.For<IClassificationCostEstimator>();

        ctx.Services.AddSingleton(captureService);
        ctx.Services.AddSingleton(skillRegistry);
        ctx.Services.AddSingleton(costEstimator);
        ctx.Services.AddSingleton(Options.Create(new DemoTraceOptions { Enabled = false }));
        ctx.RenderComponent<MudPopoverProvider>();

        skillRegistry.GetHealthAsync(Arg.Any<CancellationToken>())
            .Returns(new SkillHealth[] { new("Books", HealthStatus.Healthy, 10) });

        var id = Guid.NewGuid();
        var trace = new ClassifierTrace(ClassifierKind.Ai, 900, "OpenRouter", "gemma:free", 80, 12);
        captureService.GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(new Capture(id, ChannelKind.Web, "AI classified capture", DateTimeOffset.UtcNow,
                LifecycleStage.Classified, "Books", ClassifierTrace: trace));

        var cut = ctx.RenderComponent<CaptureDetail>(p => p.Add(c => c.Id, id));

        cut.Markup.Should().NotContain("Classification trace");
    }
}

using FlowHub.Web.Components.DashboardCards;
using FlowHub.Web.Components.Pages;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;

namespace FlowHub.Web.ComponentTests.Pages;

public class DashboardTests : TestContext
{
    private readonly ICaptureService _captureService = Substitute.For<ICaptureService>();
    private readonly ISkillRegistry _skillRegistry = Substitute.For<ISkillRegistry>();
    private readonly IIntegrationHealthService _integrationHealthService = Substitute.For<IIntegrationHealthService>();

    public DashboardTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton(_captureService);
        Services.AddSingleton(_skillRegistry);
        Services.AddSingleton(_integrationHealthService);
        RenderComponent<MudPopoverProvider>();

        _captureService.GetRecentAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Capture>());
        _captureService.GetFailureCountsAsync(Arg.Any<CancellationToken>())
            .Returns(new FailureCounts(0, 0));
        _skillRegistry.GetHealthAsync(Arg.Any<CancellationToken>())
            .Returns(Array.Empty<SkillHealth>());
        _integrationHealthService.GetHealthAsync(Arg.Any<CancellationToken>())
            .Returns(Array.Empty<IntegrationHealth>());
    }

    [Fact]
    public void Render_LoadsAllFourServicesOnInit()
    {
        RenderComponent<Dashboard>();

        _captureService.Received(1).GetRecentAsync(10, Arg.Any<CancellationToken>());
        _captureService.Received(1).GetFailureCountsAsync(Arg.Any<CancellationToken>());
        _skillRegistry.Received(1).GetHealthAsync(Arg.Any<CancellationToken>());
        _integrationHealthService.Received(1).GetHealthAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Render_LoadFailure_ShowsErrorAlertWithRetry()
    {
        _captureService.GetRecentAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns<IReadOnlyList<Capture>>(_ => throw new InvalidOperationException("db offline"));

        var cut = RenderComponent<Dashboard>();

        cut.Markup.Should().Contain("Could not load dashboard data");
        cut.Markup.Should().Contain("db offline");
        cut.Markup.Should().Contain("Retry");
    }

    [Fact]
    public async Task OrphanClick_NavigatesToOrphansFilter()
    {
        var cut = RenderComponent<Dashboard>();
        var nav = Services.GetRequiredService<NavigationManager>();
        // The Dashboard wires NeedsAttentionCard.OnOrphanClick → NavigateToOrphans.
        var card = cut.FindComponent<NeedsAttentionCard>();

        await cut.InvokeAsync(() => card.Instance.OnOrphanClick.InvokeAsync());

        nav.Uri.Should().EndWith("/captures?lc=orphan");
    }

    [Fact]
    public async Task UnhandledClick_NavigatesToUnhandledFilter()
    {
        var cut = RenderComponent<Dashboard>();
        var nav = Services.GetRequiredService<NavigationManager>();
        var card = cut.FindComponent<NeedsAttentionCard>();

        await cut.InvokeAsync(() => card.Instance.OnUnhandledClick.InvokeAsync());

        nav.Uri.Should().EndWith("/captures?lc=unhandled");
    }

    [Fact]
    public async Task ViewAllCapturesClick_NavigatesToCapturesList()
    {
        var cut = RenderComponent<Dashboard>();
        var nav = Services.GetRequiredService<NavigationManager>();
        var card = cut.FindComponent<RecentCapturesCard>();

        await cut.InvokeAsync(() => card.Instance.OnViewAllClick.InvokeAsync());

        nav.Uri.Should().EndWith("/captures");
    }

    [Fact]
    public async Task RowClick_NavigatesToCaptureDetail()
    {
        var cut = RenderComponent<Dashboard>();
        var nav = Services.GetRequiredService<NavigationManager>();
        var card = cut.FindComponent<RecentCapturesCard>();
        var id = Guid.NewGuid();

        await cut.InvokeAsync(() => card.Instance.OnRowClick.InvokeAsync(id));

        nav.Uri.Should().EndWith($"/captures/{id}");
    }

    [Fact]
    public async Task SkillsManageClick_NavigatesToSkillsPage()
    {
        var cut = RenderComponent<Dashboard>();
        var nav = Services.GetRequiredService<NavigationManager>();
        var card = cut.FindComponent<SkillHealthCard>();

        await cut.InvokeAsync(() => card.Instance.OnManageClick.InvokeAsync());

        nav.Uri.Should().EndWith("/skills");
    }

    [Fact]
    public async Task IntegrationsManageClick_NavigatesToIntegrationsPage()
    {
        var cut = RenderComponent<Dashboard>();
        var nav = Services.GetRequiredService<NavigationManager>();
        var card = cut.FindComponent<IntegrationHealthCard>();

        await cut.InvokeAsync(() => card.Instance.OnManageClick.InvokeAsync());

        nav.Uri.Should().EndWith("/integrations");
    }
}

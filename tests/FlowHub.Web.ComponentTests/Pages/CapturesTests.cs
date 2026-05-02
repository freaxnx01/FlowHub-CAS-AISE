using FlowHub.Web.Stubs;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;

namespace FlowHub.Web.ComponentTests.Pages;

public class CapturesTests : TestContext
{
    private readonly CaptureServiceStub _captureService = new(Substitute.For<IPublishEndpoint>());

    public CapturesTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<ICaptureService>(_captureService);
        RenderComponent<MudPopoverProvider>();
    }

    [Fact]
    public void Render_ShowsAllCaptures_WhenNoFiltersApplied()
    {
        var cut = RenderComponent<FlowHub.Web.Components.Pages.Captures>();

        cut.Markup.Should().Contain("Captures");
        cut.Markup.Should().Contain("Inception");
        cut.Markup.Should().Contain("Lifecycle:");
        cut.Markup.Should().Contain("Channel:");
    }

    [Fact]
    public void Render_ShowsResultsCount()
    {
        var cut = RenderComponent<FlowHub.Web.Components.Pages.Captures>();

        cut.Markup.Should().Contain("Results: 12");
    }

    // Query param pre-selection (?lc=Orphan) is a 1-line Enum.TryParse in
    // OnInitializedAsync. Testing it in bUnit requires a full Router context
    // to parse [SupplyParameterFromQuery], which is disproportionate setup.
    // Covered by manual testing and future E2E tests (Block 5).
}

using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using IntegrationsPage = FlowHub.Web.Components.Pages.Integrations;

namespace FlowHub.Web.ComponentTests.Pages;

public class IntegrationsTests : TestContext
{
    private readonly IIntegrationHealthService _service = Substitute.For<IIntegrationHealthService>();

    public IntegrationsTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton(_service);
        RenderComponent<MudPopoverProvider>();
    }

    [Fact]
    public void Render_Empty_ShowsConfigurationHint()
    {
        _service.GetHealthAsync(Arg.Any<CancellationToken>())
            .Returns(Array.Empty<IntegrationHealth>());

        var cut = RenderComponent<IntegrationsPage>();

        cut.Markup.Should().Contain("No integrations configured yet");
    }

    [Fact]
    public void Render_WithData_ListsEveryIntegrationWithStatus()
    {
        _service.GetHealthAsync(Arg.Any<CancellationToken>())
            .Returns(new IntegrationHealth[]
            {
                new("Wallabag", HealthStatus.Healthy, DateTimeOffset.UtcNow.AddMinutes(-5), TimeSpan.FromMilliseconds(180)),
                new("Vikunja",  HealthStatus.Down,    null,                                  null),
            });

        var cut = RenderComponent<IntegrationsPage>();

        cut.Markup.Should().Contain("Wallabag");
        cut.Markup.Should().Contain("Vikunja");
        cut.Markup.Should().Contain("healthy");
        cut.Markup.Should().Contain("down");
    }

    [Fact]
    public void Render_LoadFailure_ShowsErrorAlertWithRetry()
    {
        _service.GetHealthAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IReadOnlyList<IntegrationHealth>>(new InvalidOperationException("upstream timeout")));

        var cut = RenderComponent<IntegrationsPage>();

        cut.Markup.Should().Contain("Could not load integrations");
        cut.Markup.Should().Contain("upstream timeout");
        cut.Markup.Should().Contain("Retry");
    }
}

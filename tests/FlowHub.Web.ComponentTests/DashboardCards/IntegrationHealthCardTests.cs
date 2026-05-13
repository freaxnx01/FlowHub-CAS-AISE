using FlowHub.Web.Components.DashboardCards;
using MudBlazor.Services;

namespace FlowHub.Web.ComponentTests.DashboardCards;

public class IntegrationHealthCardTests : TestContext
{
    public IntegrationHealthCardTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void Render_Loading_ShowsSkeletons_WhenIntegrationsIsNull()
    {
        var cut = RenderComponent<IntegrationHealthCard>(p => p.Add(c => c.Integrations, null));

        cut.FindAll(".mud-skeleton").Should().NotBeEmpty();
    }

    [Fact]
    public void Render_Empty_ShowsNoIntegrationsMessage()
    {
        var cut = RenderComponent<IntegrationHealthCard>(p =>
            p.Add(c => c.Integrations, Array.Empty<IntegrationHealth>()));

        cut.Markup.Should().Contain("No integrations configured yet");
    }

    [Fact]
    public void Render_WithData_ListsEveryIntegrationByName()
    {
        var integrations = new[]
        {
            new IntegrationHealth("Wallabag", HealthStatus.Healthy, DateTimeOffset.UtcNow.AddMinutes(-2), TimeSpan.FromMilliseconds(120)),
            new IntegrationHealth("Vikunja",  HealthStatus.Degraded, DateTimeOffset.UtcNow.AddHours(-1), null),
            new IntegrationHealth("Obsidian", HealthStatus.Down,     null,                                null),
        };

        var cut = RenderComponent<IntegrationHealthCard>(p => p.Add(c => c.Integrations, integrations));

        cut.Markup.Should().Contain("Wallabag");
        cut.Markup.Should().Contain("Vikunja");
        cut.Markup.Should().Contain("Obsidian");
        cut.Markup.Should().Contain("degraded");
        cut.Markup.Should().Contain("down");
    }

    [Fact]
    public void Click_ManageIntegrations_RaisesManageCallback()
    {
        var clicked = false;
        var cut = RenderComponent<IntegrationHealthCard>(p =>
        {
            p.Add(c => c.Integrations, Array.Empty<IntegrationHealth>());
            p.Add(c => c.OnManageClick, EventCallback.Factory.Create(this, () => clicked = true));
        });

        cut.FindAll("button").First(b => b.TextContent.Contains("Manage integrations")).Click();

        clicked.Should().BeTrue();
    }
}

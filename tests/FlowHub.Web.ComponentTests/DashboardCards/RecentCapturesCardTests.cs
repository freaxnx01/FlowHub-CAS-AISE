using FlowHub.Web.Components.DashboardCards;
using MudBlazor;
using MudBlazor.Services;

namespace FlowHub.Web.ComponentTests.DashboardCards;

public class RecentCapturesCardTests : TestContext
{
    public RecentCapturesCardTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
        RenderComponent<MudPopoverProvider>();
    }

    [Fact]
    public void Render_Loading_ShowsSkeletons_WhenCapturesIsNull()
    {
        var cut = RenderComponent<RecentCapturesCard>(p => p.Add(c => c.Captures, null));

        cut.FindAll(".mud-skeleton").Should().NotBeEmpty();
    }

    [Fact]
    public void Render_Empty_ShowsEmptyStateMessage()
    {
        var cut = RenderComponent<RecentCapturesCard>(p =>
            p.Add(c => c.Captures, Array.Empty<Capture>()));

        cut.Markup.Should().Contain("No captures yet");
    }

    [Fact]
    public void Render_WithData_ShowsCaptureContent()
    {
        var captures = new[]
        {
            new Capture(Guid.NewGuid(), ChannelKind.Web, "Read this article", DateTimeOffset.UtcNow,
                LifecycleStage.Completed, "Articles", null, "Wikipedia entry"),
        };

        var cut = RenderComponent<RecentCapturesCard>(p => p.Add(c => c.Captures, captures));

        cut.Markup.Should().Contain("Read this article");
        cut.Markup.Should().Contain("Wikipedia entry");
        cut.Markup.Should().Contain("Articles");
    }

    [Fact]
    public void Click_ViewAll_RaisesViewAllCallback()
    {
        var clicked = false;
        var cut = RenderComponent<RecentCapturesCard>(p =>
        {
            p.Add(c => c.Captures, Array.Empty<Capture>());
            p.Add(c => c.OnViewAllClick, EventCallback.Factory.Create(this, () => clicked = true));
        });

        cut.FindAll("button").First(b => b.TextContent.Contains("View all")).Click();

        clicked.Should().BeTrue();
    }
}

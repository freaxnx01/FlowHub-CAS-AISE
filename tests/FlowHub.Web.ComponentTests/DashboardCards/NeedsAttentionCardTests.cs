using FlowHub.Web.Components.DashboardCards;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;

namespace FlowHub.Web.ComponentTests.DashboardCards;

public class NeedsAttentionCardTests : TestContext
{
    public NeedsAttentionCardTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void Render_Loading_ShowsSkeletons_WhenCountsIsNull()
    {
        var cut = RenderComponent<NeedsAttentionCard>(p => p.Add(c => c.Counts, null));

        cut.FindAll(".mud-skeleton").Should().NotBeEmpty();
    }

    [Fact]
    public void Render_AllClear_ShowsCalmMessage_WhenAllCountsZero()
    {
        var cut = RenderComponent<NeedsAttentionCard>(p =>
            p.Add(c => c.Counts, new FailureCounts(0, 0)));

        cut.Markup.Should().Contain("All captures routed successfully");
    }

    [Fact]
    public void Render_WithFailures_ShowsBothActionableButtons()
    {
        var cut = RenderComponent<NeedsAttentionCard>(p =>
            p.Add(c => c.Counts, new FailureCounts(3, 1)));

        cut.Markup.Should().Contain("3 orphan captures");
        cut.Markup.Should().Contain("1 unhandled capture");
    }

    [Fact]
    public void Click_OrphanButton_RaisesOrphanCallback()
    {
        var clicked = false;
        var cut = RenderComponent<NeedsAttentionCard>(p =>
        {
            p.Add(c => c.Counts, new FailureCounts(2, 0));
            p.Add(c => c.OnOrphanClick, EventCallback.Factory.Create(this, () => clicked = true));
        });

        cut.FindAll("button").First(b => b.TextContent.Contains("orphan")).Click();

        clicked.Should().BeTrue();
    }

    [Fact]
    public void Click_UnhandledButton_RaisesUnhandledCallback()
    {
        var clicked = false;
        var cut = RenderComponent<NeedsAttentionCard>(p =>
        {
            p.Add(c => c.Counts, new FailureCounts(0, 5));
            p.Add(c => c.OnUnhandledClick, EventCallback.Factory.Create(this, () => clicked = true));
        });

        cut.FindAll("button").First(b => b.TextContent.Contains("unhandled")).Click();

        clicked.Should().BeTrue();
    }
}

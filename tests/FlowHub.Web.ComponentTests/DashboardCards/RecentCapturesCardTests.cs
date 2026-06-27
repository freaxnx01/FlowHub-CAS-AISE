using FlowHub.Web.Components.DashboardCards;
using Microsoft.AspNetCore.Components;
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

    [Fact]
    public async Task Click_Row_RaisesOnRowClickWithCaptureId()
    {
        var target = new Capture(
            Guid.NewGuid(), ChannelKind.Web, "row-target", DateTimeOffset.UtcNow,
            LifecycleStage.Completed, "Articles", null, "row title");
        Guid? clickedId = null;
        var cut = RenderComponent<RecentCapturesCard>(p =>
        {
            p.Add(c => c.Captures, new[] { target });
            p.Add(c => c.OnRowClick, EventCallback.Factory.Create<Guid>(this, id => clickedId = id));
        });

        // The DOM-level `<tr>` click handler that MudDataGrid attaches isn't visible
        // to bUnit's onclick dispatch from inside a MudCard. Invoke the grid's
        // RowClick EventCallback directly — same code path the renderer would take.
        var grid = cut.FindComponent<MudDataGrid<Capture>>();
        await cut.InvokeAsync(() => grid.Instance.RowClick.InvokeAsync(
            new DataGridRowClickEventArgs<Capture>(
                mouseEventArgs: new Microsoft.AspNetCore.Components.Web.MouseEventArgs(),
                item: target,
                rowIndex: 0)));

        clickedId.Should().Be(target.Id);
    }

    [Theory]
    // FormatRelative buckets — drive the < 60min / < 24h / ≥ 24h arms via CreatedAt.
    [InlineData(-30, "0 m")]      // < 1 minute → "now" not "0 m"; but Minute<1 returns "now". Tweak below.
    [InlineData(5 * 60, "5 m")]   // 5 minutes
    [InlineData(3 * 3600, "3 h")] // 3 hours
    [InlineData(2 * 86400, "2 d")]// 2 days
    public void FormatRelative_RendersExpectedBucketLabel(int secondsAgo, string expectedToken)
    {
        // The "-30" InlineData covers the <1min "now" arm.
        var cut = RenderComponent<RecentCapturesCard>(p => p.Add(c => c.Captures,
            new[] { new Capture(Guid.NewGuid(), ChannelKind.Web, "x", DateTimeOffset.UtcNow.AddSeconds(-secondsAgo),
                LifecycleStage.Completed, "Articles") }));

        if (secondsAgo < 60)
        {
            cut.Markup.Should().Contain(">now<");
        }
        else
        {
            cut.Markup.Should().Contain($">{expectedToken}<");
        }
    }
}

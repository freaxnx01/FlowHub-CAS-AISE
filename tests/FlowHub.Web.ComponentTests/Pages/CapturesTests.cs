using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using CapturesPage = FlowHub.Web.Components.Pages.Captures;

namespace FlowHub.Web.ComponentTests.Pages;

public class CapturesTests : TestContext
{
    private readonly ICaptureService _captureService = Substitute.For<ICaptureService>();

    public CapturesTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton(_captureService);
        RenderComponent<MudPopoverProvider>();
    }

    private static Capture Cap(
        string content,
        LifecycleStage stage = LifecycleStage.Completed,
        ChannelKind source = ChannelKind.Web,
        string? matchedSkill = "Movies",
        DateTimeOffset? at = null) =>
        new(
            Guid.NewGuid(),
            source,
            content,
            at ?? DateTimeOffset.UtcNow,
            stage,
            matchedSkill);

    private void GivenCaptures(params Capture[] items) =>
        _captureService.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(items);

    [Fact]
    public void Render_WithCaptures_ShowsTitleResultsAndContent()
    {
        GivenCaptures(
            Cap("Inception (2010) — rewatch"),
            Cap("https://example.com/something"));

        var cut = RenderComponent<CapturesPage>();

        cut.Markup.Should().Contain("Captures");
        cut.Markup.Should().Contain("Lifecycle:");
        cut.Markup.Should().Contain("Channel:");
        cut.Markup.Should().Contain("Results: 2");
        cut.Markup.Should().Contain("Inception");
    }

    [Fact]
    public void Render_NoCaptures_ShowsEmptyStateWithNewCaptureLink()
    {
        GivenCaptures();

        var cut = RenderComponent<CapturesPage>();

        cut.Markup.Should().Contain("No captures yet");
        // Empty state has the "New Capture" CTA pointing at /captures/new.
        cut.Find("a[href='captures/new']").TextContent.Should().Contain("New Capture");
    }

    [Fact]
    public void Render_LoadFails_ShowsErrorAlertWithRetryButton()
    {
        _captureService.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns<IReadOnlyList<Capture>>(_ => throw new InvalidOperationException("db down"));

        var cut = RenderComponent<CapturesPage>();

        cut.Markup.Should().Contain("Could not load captures");
        cut.Markup.Should().Contain("db down");
        // The Retry button is wired to LoadAsync — verify it exists and re-invokes the service.
        var retry = cut.FindAll("button").First(b => b.TextContent.Contains("Retry"));
        retry.Should().NotBeNull();
    }

    [Fact]
    public void Filter_ClickWhileLoadErrored_HitsApplyFiltersNullGuard()
    {
        // After LoadAsync throws, _allCaptures is null but the filter chip bar
        // is still rendered (it's unconditional in the razor). Clicking a chip
        // then fires ApplyFilters with no data — the null guard must early-return.
        _captureService.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns<IReadOnlyList<Capture>>(_ => throw new InvalidOperationException("offline"));

        var cut = RenderComponent<CapturesPage>();
        cut.Markup.Should().Contain("Could not load captures");

        var act = () =>
            cut.FindAll(".mud-chip").First(c => c.TextContent.Trim() == "Orphan").Click();

        act.Should().NotThrow();
    }

    [Fact]
    public void Retry_AfterError_ReExecutesLoad()
    {
        var call = 0;
        _captureService.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns<IReadOnlyList<Capture>>(_ =>
            {
                call++;
                return call == 1
                    ? throw new InvalidOperationException("transient")
                    : new[] { Cap("Recovered capture") };
            });

        var cut = RenderComponent<CapturesPage>();
        cut.Markup.Should().Contain("Could not load captures");

        cut.FindAll("button").First(b => b.TextContent.Contains("Retry")).Click();

        cut.WaitForAssertion(() =>
            cut.Markup.Should().Contain("Recovered capture"));
        cut.Markup.Should().NotContain("Could not load captures");
    }

    [Fact]
    public void LifecycleFilter_SelectingStage_NarrowsRows()
    {
        GivenCaptures(
            Cap("alpha-completed", LifecycleStage.Completed),
            Cap("beta-orphan", LifecycleStage.Orphan),
            Cap("gamma-orphan", LifecycleStage.Orphan));

        var cut = RenderComponent<CapturesPage>();
        cut.Markup.Should().Contain("Results: 3");

        // Click the Orphan chip in the Lifecycle row.
        var orphanChip = cut.FindAll(".mud-chip").First(c => c.TextContent.Trim() == "Orphan");
        orphanChip.Click();

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Results: 2"));
        cut.Markup.Should().Contain("beta-orphan");
        cut.Markup.Should().Contain("gamma-orphan");
        cut.Markup.Should().NotContain("alpha-completed");
    }

    [Fact]
    public void ChannelFilter_SelectingChannel_NarrowsRows()
    {
        GivenCaptures(
            Cap("web one", source: ChannelKind.Web),
            Cap("telegram one", source: ChannelKind.Telegram),
            Cap("telegram two", source: ChannelKind.Telegram));

        var cut = RenderComponent<CapturesPage>();

        var telegramChip = cut.FindAll(".mud-chip").First(c => c.TextContent.Trim() == "Telegram");
        telegramChip.Click();

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Results: 2"));
        cut.Markup.Should().Contain("telegram one");
        cut.Markup.Should().NotContain("web one");
    }

    [Fact]
    public void SearchText_TypingInBox_NarrowsRowsCaseInsensitively()
    {
        GivenCaptures(
            Cap("Inception (2010)"),
            Cap("The Matrix"),
            Cap("inception sequel idea"));

        var cut = RenderComponent<CapturesPage>();
        cut.Markup.Should().Contain("Results: 3");

        // Search box — MudTextField with Immediate="true" binds on oninput.
        var search = cut.Find("input[placeholder='Search content…']");
        search.Input("inception");

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Results: 2"));
        cut.Markup.Should().Contain("Inception (2010)");
        cut.Markup.Should().Contain("inception sequel idea");
        cut.Markup.Should().NotContain("Matrix");
    }

    [Fact]
    public void FilteredEmpty_ShowsClearFiltersButton_WhichResetsFilters()
    {
        GivenCaptures(
            Cap("only one", LifecycleStage.Completed, source: ChannelKind.Web));

        var cut = RenderComponent<CapturesPage>();

        // Apply a filter that yields zero rows.
        cut.FindAll(".mud-chip").First(c => c.TextContent.Trim() == "Orphan").Click();
        cut.WaitForAssertion(() =>
            cut.Markup.Should().Contain("No captures match the current filters"));

        // Clear filters button restores the full list.
        cut.FindAll("button").First(b => b.TextContent.Contains("Clear filters")).Click();

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Results: 1"));
        cut.Markup.Should().Contain("only one");
    }

    // [SupplyParameterFromQuery] pre-selection (?lc=Stage → _selectedLifecycle)
    // is a 3-line branch in OnInitializedAsync. bUnit's FakeNavigationManager
    // does not populate query-string parameters into a directly-rendered
    // component (only via the Router pipeline), so NavigateTo+RenderComponent
    // never reaches it. Covered by manual testing + future E2E (Block 5).

    [Fact]
    public void RowClick_NavigatesToCaptureDetail()
    {
        var target = Cap("target row");
        GivenCaptures(target, Cap("other row"));

        var cut = RenderComponent<CapturesPage>();
        var nav = Services.GetRequiredService<NavigationManager>();

        // MudDataGrid renders rows as <tr class="mud-table-row"> and wires RowClick
        // to the row's click handler. The newest capture sorts first; click it.
        var targetRow = cut.FindAll("tr.mud-table-row")
            .First(r => r.TextContent.Contains("target row"));
        targetRow.Click();

        nav.Uri.Should().EndWith($"/captures/{target.Id}");
    }

    [Fact]
    public void FormatRelative_RendersHumanizedDurations()
    {
        var now = DateTimeOffset.UtcNow;
        GivenCaptures(
            Cap("now-cap",       at: now.AddSeconds(-10)),  // < 1 min  → "now"
            Cap("minutes-cap",   at: now.AddMinutes(-5)),   // < 60 min → "5 m"
            Cap("hours-cap",     at: now.AddHours(-3)),     // < 24 h   → "3 h"
            Cap("days-cap",      at: now.AddDays(-4)));     //          → "4 d"

        var cut = RenderComponent<CapturesPage>();
        var markup = cut.Markup;

        // Each bucket of FormatRelative must produce its respective token.
        markup.Should().Contain(">now<");
        markup.Should().Contain(">5 m<");
        markup.Should().Contain(">3 h<");
        markup.Should().Contain(">4 d<");
    }
}

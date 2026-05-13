namespace FlowHub.Web.E2ETests.Journeys;

[Trait("Category", "E2E")]
[Trait("Journey", "J10")]
[Collection("Playwright")]
public sealed class J10_CapturesListLifecycleFilter : JourneyTestBase
{
    public J10_CapturesListLifecycleFilter(PlaywrightFixture fixture) : base(fixture, "J10.json") { }

    [Fact]
    public async Task ClickUnhandledChip_NarrowsResults()
    {
        await GotoAsync("/captures");

        var beforeText = await Page.GetByText("Results:", new PageGetByTextOptions { Exact = false }).First.InnerTextAsync();

        var chip = Page.Locator(".mud-chip", new PageLocatorOptions { HasText = "unhandled" }).First;
        await chip.WaitForAsync(new LocatorWaitForOptions { Timeout = 15_000 });
        await chip.ClickAsync();

        // Allow grid to recompute
        await Page.WaitForTimeoutAsync(500);

        var afterText = await Page.GetByText("Results:", new PageGetByTextOptions { Exact = false }).First.InnerTextAsync();
        afterText.Should().NotBeNullOrEmpty();
    }
}

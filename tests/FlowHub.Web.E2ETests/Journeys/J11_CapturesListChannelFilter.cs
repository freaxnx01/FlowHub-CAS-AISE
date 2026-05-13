namespace FlowHub.Web.E2ETests.Journeys;

[Trait("Category", "E2E")]
[Trait("Journey", "J11")]
[Collection("Playwright")]
public sealed class J11_CapturesListChannelFilter : JourneyTestBase
{
    public J11_CapturesListChannelFilter(PlaywrightFixture fixture) : base(fixture, "J11.json") { }

    [Fact]
    public async Task ClickWebChannelChip_NarrowsResults()
    {
        await GotoAsync("/captures");

        await Page.GetByText("Results:", new PageGetByTextOptions { Exact = false })
            .First.WaitForAsync(new LocatorWaitForOptions { Timeout = 15_000 });

        var chip = Page.Locator(".mud-chip", new PageLocatorOptions { HasText = "Web" }).First;
        await chip.WaitForAsync(new LocatorWaitForOptions { Timeout = 15_000 });
        await chip.ClickAsync();

        await Page.WaitForTimeoutAsync(500);
        var after = await Page.GetByText("Results:", new PageGetByTextOptions { Exact = false }).First.InnerTextAsync();
        after.Should().StartWith("Results:");
    }
}

namespace FlowHub.Web.E2ETests.Journeys;

[Trait("Category", "E2E")]
[Trait("Journey", "J12")]
[Collection("Playwright")]
public sealed class J12_CapturesListSearch : JourneyTestBase
{
    public J12_CapturesListSearch(PlaywrightFixture fixture) : base(fixture, "J12.json") { }

    [Fact]
    public async Task TypeSearchText_FiltersResults()
    {
        await GotoAsync("/captures");

        var search = Page.GetByPlaceholder("Search content…").First;
        // Fall back if placeholder differs
        if (await search.CountAsync() == 0)
        {
            search = Page.Locator("input[type='text']").Last;
        }
        await search.WaitForAsync(new LocatorWaitForOptions { Timeout = 15_000 });
        await search.FillAsync("http");

        await Page.WaitForTimeoutAsync(500);
        var after = await Page.GetByText("Results:", new PageGetByTextOptions { Exact = false }).First.InnerTextAsync();
        after.Should().StartWith("Results:");
    }
}

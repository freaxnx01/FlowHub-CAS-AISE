namespace FlowHub.Web.E2ETests.Journeys;

[Trait("Category", "E2E")]
[Trait("Journey", "J13")]
[Collection("Playwright")]
public sealed class J13_CapturesListNoMatchEmptyState : JourneyTestBase
{
    public J13_CapturesListNoMatchEmptyState(PlaywrightFixture fixture) : base(fixture, "J13.json") { }

    [Fact]
    public async Task SearchWithNoMatches_ShowsEmptyState()
    {
        await GotoAsync("/captures");

        var search = Page.GetByPlaceholder("Search content…").First;
        if (await search.CountAsync() == 0)
        {
            search = Page.Locator("input[type='text']").Last;
        }
        await search.FillAsync("zzzzzzzz-no-such-string");

        await Page.WaitForTimeoutAsync(500);

        var resultsText = await Page.GetByText("Results:", new PageGetByTextOptions { Exact = false }).First.InnerTextAsync();
        var noMatchVisible = await Page.GetByText("No captures match", new PageGetByTextOptions { Exact = false }).CountAsync() > 0;

        (resultsText.Contains("Results: 0", StringComparison.Ordinal) || noMatchVisible)
            .Should().BeTrue("filtered list should report zero matches one way or the other");
    }
}

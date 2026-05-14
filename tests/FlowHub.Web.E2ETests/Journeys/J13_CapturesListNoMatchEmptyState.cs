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

        // Either the empty-state panel renders OR the body grid has zero rows.
        await Page.WaitForFunctionAsync(@"
            () => document.body.innerText.includes('No captures match')
                || document.querySelectorAll('tbody .mud-table-row').length === 0",
            null,
            new PageWaitForFunctionOptions { Timeout = 10_000 });
    }
}

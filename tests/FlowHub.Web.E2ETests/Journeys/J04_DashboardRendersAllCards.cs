namespace FlowHub.Web.E2ETests.Journeys;

[Trait("Category", "E2E")]
[Trait("Journey", "J04")]
[Collection("Playwright")]
public sealed class J04_DashboardRendersAllCards : JourneyTestBase
{
    public J04_DashboardRendersAllCards(PlaywrightFixture fixture) : base(fixture, "J04.json") { }

    [Fact]
    public async Task Dashboard_ShowsAllFourCardTitles()
    {
        await GotoAsync("/");

        foreach (var title in new[] { "Needs attention", "Recent captures", "Skill health", "Integration health" })
        {
            await Page.GetByText(title, new PageGetByTextOptions { Exact = false })
                .First.WaitForAsync(new LocatorWaitForOptions { Timeout = 15_000 });
        }
    }
}

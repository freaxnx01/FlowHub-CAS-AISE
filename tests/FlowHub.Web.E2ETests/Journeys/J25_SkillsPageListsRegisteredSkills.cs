namespace FlowHub.Web.E2ETests.Journeys;

[Trait("Category", "E2E")]
[Trait("Journey", "J25")]
[Collection("Playwright")]
public sealed class J25_SkillsPageListsRegisteredSkills : JourneyTestBase
{
    public J25_SkillsPageListsRegisteredSkills(PlaywrightFixture fixture) : base(fixture, "J25.json") { }

    [Fact]
    public async Task SkillsPage_ContainsBooksAndMovies()
    {
        await GotoAsync("/skills");

        await Page.GetByText("Books", new PageGetByTextOptions { Exact = false })
            .First.WaitForAsync(new LocatorWaitForOptions { Timeout = 15_000 });
        await Page.GetByText("Movies", new PageGetByTextOptions { Exact = false })
            .First.WaitForAsync(new LocatorWaitForOptions { Timeout = 15_000 });
    }
}

namespace FlowHub.Web.E2ETests.Journeys;

[Trait("Category", "E2E")]
[Trait("Journey", "J26")]
[Collection("Playwright")]
public sealed class J26_SkillsPageLoadFailure : JourneyTestBase
{
    public J26_SkillsPageLoadFailure(PlaywrightFixture fixture) : base(fixture, "J26.json") { }

    [Fact]
    public async Task SkillsPage_ShowsErrorAlertWhenRegistryFails()
    {
        // Cannot force the failure path against live persistence; bUnit
        // (SkillsTests.Render_LoadFailure_ShowsErrorAlertWithRetry) covers it.
        // This spec is intentionally red until a feature toggle / fault-injection
        // hook lets us trigger the error branch from the test side.
        await GotoAsync("/skills");

        await Page.Locator(".mud-alert", new PageLocatorOptions { HasText = "Could not load skills" })
            .First.WaitForAsync(new LocatorWaitForOptions { Timeout = 5_000 });
    }
}

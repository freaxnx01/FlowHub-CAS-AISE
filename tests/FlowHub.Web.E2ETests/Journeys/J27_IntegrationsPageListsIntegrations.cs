namespace FlowHub.Web.E2ETests.Journeys;

[Trait("Category", "E2E")]
[Trait("Journey", "J27")]
[Collection("Playwright")]
public sealed class J27_IntegrationsPageListsIntegrations : JourneyTestBase
{
    public J27_IntegrationsPageListsIntegrations(PlaywrightFixture fixture) : base(fixture, "J27.json") { }

    [Fact]
    public async Task IntegrationsPage_ContainsWallabagAndVikunja()
    {
        await GotoAsync("/integrations");

        await Page.GetByText("Wallabag", new PageGetByTextOptions { Exact = false })
            .First.WaitForAsync(new LocatorWaitForOptions { Timeout = 15_000 });
        await Page.GetByText("Vikunja", new PageGetByTextOptions { Exact = false })
            .First.WaitForAsync(new LocatorWaitForOptions { Timeout = 15_000 });
    }
}

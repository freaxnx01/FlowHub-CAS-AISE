namespace FlowHub.Web.E2ETests.Journeys;

[Trait("Category", "E2E")]
[Trait("Journey", "J28")]
[Collection("Playwright")]
public sealed class J28_IntegrationsPageLoadFailure : JourneyTestBase
{
    public J28_IntegrationsPageLoadFailure(PlaywrightFixture fixture) : base(fixture, "J28.json") { }

    [Fact]
    public async Task IntegrationsPage_ShowsErrorAlertWhenServiceFails()
    {
        // Cannot force the failure path against live persistence; bUnit
        // (IntegrationsTests.Render_LoadFailure_ShowsErrorAlertWithRetry) covers it.
        // Intentionally red until a fault-injection hook is available.
        await GotoAsync("/integrations");

        await Page.Locator(".mud-alert", new PageLocatorOptions { HasText = "Could not load integrations" })
            .First.WaitForAsync(new LocatorWaitForOptions { Timeout = 5_000 });
    }
}

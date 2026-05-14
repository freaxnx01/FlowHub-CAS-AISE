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
        // Arms FlowHub.Web's IFaultInjector via /test/faults/integrations/arm
        // so the IIntegrationHealthService decorator throws on the next call.
        using var http = new HttpClient { BaseAddress = new Uri(Fixture.BaseUrl) };
        var arm = await http.PostAsync("/test/faults/integrations/arm", content: null);
        arm.EnsureSuccessStatusCode();
        try
        {
            await GotoAsync("/integrations");

            await Page.Locator(".mud-alert", new PageLocatorOptions { HasText = "Could not load integrations" })
                .First.WaitForAsync(new LocatorWaitForOptions { Timeout = 15_000 });
        }
        finally
        {
            (await http.PostAsync("/test/faults/integrations/disarm", content: null)).EnsureSuccessStatusCode();
        }
    }
}

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
        // Arms FlowHub.Web's IFaultInjector via /test/faults/skills/arm so the
        // server-side ISkillRegistry decorator throws on the next call. Disarmed
        // in finally so a failed assertion can't poison subsequent tests.
        using var http = new HttpClient { BaseAddress = new Uri(Fixture.BaseUrl) };
        var arm = await http.PostAsync("/test/faults/skills/arm", content: null);
        arm.EnsureSuccessStatusCode();
        try
        {
            await GotoAsync("/skills");

            await Page.Locator(".mud-alert", new PageLocatorOptions { HasText = "Could not load skills" })
                .First.WaitForAsync(new LocatorWaitForOptions { Timeout = 15_000 });
        }
        finally
        {
            (await http.PostAsync("/test/faults/skills/disarm", content: null)).EnsureSuccessStatusCode();
        }
    }
}

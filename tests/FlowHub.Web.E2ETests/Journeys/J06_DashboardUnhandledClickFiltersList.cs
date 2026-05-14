using System.Text.RegularExpressions;

namespace FlowHub.Web.E2ETests.Journeys;

[Trait("Category", "E2E")]
[Trait("Journey", "J06")]
[Collection("Playwright")]
public sealed class J06_DashboardUnhandledClickFiltersList : JourneyTestBase
{
    public J06_DashboardUnhandledClickFiltersList(PlaywrightFixture fixture) : base(fixture, "J06.json") { }

    [Fact]
    public async Task ClickUnhandledCount_NavigatesToCapturesFilteredByUnhandled()
    {
        await GotoAsync("/");

        var unhandledButton = Page.Locator("button", new PageLocatorOptions { HasTextRegex = new Regex(@"\d+\s+unhandled", RegexOptions.IgnoreCase) }).First;
        await unhandledButton.WaitForAsync(new LocatorWaitForOptions { Timeout = 15_000 });
        await unhandledButton.ClickAsync();

        await Page.WaitForURLAsync(new Regex(@"/captures\?lc=[Uu]nhandled"), new PageWaitForURLOptions { Timeout = 15_000 });
    }
}

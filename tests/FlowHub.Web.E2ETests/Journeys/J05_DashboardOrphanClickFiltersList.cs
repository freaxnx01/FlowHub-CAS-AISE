using System.Text.RegularExpressions;

namespace FlowHub.Web.E2ETests.Journeys;

[Trait("Category", "E2E")]
[Trait("Journey", "J05")]
[Collection("Playwright")]
public sealed class J05_DashboardOrphanClickFiltersList : JourneyTestBase
{
    public J05_DashboardOrphanClickFiltersList(PlaywrightFixture fixture) : base(fixture, "J05.json") { }

    [Fact]
    public async Task ClickOrphanCount_NavigatesToCapturesFilteredByOrphan()
    {
        await GotoAsync("/");

        var orphanButton = Page.Locator("button", new PageLocatorOptions { HasTextRegex = new Regex(@"\d+\s+orphan", RegexOptions.IgnoreCase) }).First;
        await orphanButton.WaitForAsync(new LocatorWaitForOptions { Timeout = 15_000 });
        await orphanButton.ClickAsync();

        await Page.WaitForURLAsync(new Regex(@"/captures\?lc=[Oo]rphan"), new PageWaitForURLOptions { Timeout = 15_000 });
    }
}

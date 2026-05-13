using System.Text.RegularExpressions;

namespace FlowHub.Web.E2ETests.Journeys;

[Trait("Category", "E2E")]
[Trait("Journey", "J08")]
[Collection("Playwright")]
public sealed class J08_DashboardViewAllNavigation : JourneyTestBase
{
    public J08_DashboardViewAllNavigation(PlaywrightFixture fixture) : base(fixture, "J08.json") { }

    [Fact]
    public async Task ClickViewAll_NavigatesToCapturesList()
    {
        await GotoAsync("/");

        var viewAll = Page.GetByRole(AriaRole.Button, new() { Name = "View all" }).First;
        await viewAll.WaitForAsync(new LocatorWaitForOptions { Timeout = 15_000 });
        await viewAll.ClickAsync();

        await Page.WaitForURLAsync(new Regex(@"/captures$"), new PageWaitForURLOptions { Timeout = 15_000 });
    }
}

using System.Text.RegularExpressions;

namespace FlowHub.Web.E2ETests.Journeys;

[Trait("Category", "E2E")]
[Trait("Journey", "J07")]
[Collection("Playwright")]
public sealed class J07_DashboardRowClickOpensDetail : JourneyTestBase
{
    public J07_DashboardRowClickOpensDetail(PlaywrightFixture fixture) : base(fixture, "J07.json") { }

    [Fact]
    public async Task ClickFirstRecentCapturesRow_NavigatesToCaptureDetail()
    {
        await GotoAsync("/");

        // Wait for the Recent Captures grid to populate
        var firstRow = Page.Locator("tbody .mud-table-row").First;
        await firstRow.WaitForAsync(new LocatorWaitForOptions { Timeout = 15_000 });
        await firstRow.ClickAsync();

        await Page.WaitForURLAsync(new Regex(@"/captures/[0-9a-fA-F-]{36}"),
            new PageWaitForURLOptions { Timeout = 15_000 });
    }
}

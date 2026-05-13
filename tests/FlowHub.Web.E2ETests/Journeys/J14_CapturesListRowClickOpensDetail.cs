using System.Text.RegularExpressions;

namespace FlowHub.Web.E2ETests.Journeys;

[Trait("Category", "E2E")]
[Trait("Journey", "J14")]
[Collection("Playwright")]
public sealed class J14_CapturesListRowClickOpensDetail : JourneyTestBase
{
    public J14_CapturesListRowClickOpensDetail(PlaywrightFixture fixture) : base(fixture, "J14.json") { }

    [Fact]
    public async Task ClickFirstRow_NavigatesToCaptureDetail()
    {
        await GotoAsync("/captures");

        var firstRow = Page.Locator(".mud-table-row").First;
        await firstRow.WaitForAsync(new LocatorWaitForOptions { Timeout = 15_000 });
        await firstRow.ClickAsync();

        await Page.WaitForURLAsync(new Regex(@"/captures/[0-9a-fA-F-]{36}"),
            new PageWaitForURLOptions { Timeout = 15_000 });
    }
}

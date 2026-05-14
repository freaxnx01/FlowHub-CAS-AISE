using System.Text.RegularExpressions;

namespace FlowHub.Web.E2ETests.Journeys;

[Trait("Category", "E2E")]
[Trait("Journey", "J19")]
[Collection("Playwright")]
public sealed class J19_CaptureDetailBackLink : JourneyTestBase
{
    public J19_CaptureDetailBackLink(PlaywrightFixture fixture) : base(fixture, "J19.json") { }

    [Fact]
    public async Task ClickBackToCaptures_ReturnsToCapturesList()
    {
        await GotoAsync("/captures");

        var firstRow = Page.Locator("tbody .mud-table-row").First;
        await firstRow.WaitForAsync(new LocatorWaitForOptions { Timeout = 15_000 });
        await firstRow.ClickAsync();

        await Page.WaitForURLAsync(new Regex(@"/captures/[0-9a-fA-F-]{36}"),
            new PageWaitForURLOptions { Timeout = 15_000 });

        var back = Page.GetByRole(AriaRole.Button, new() { Name = "Back to Captures" }).First;
        await back.WaitForAsync(new LocatorWaitForOptions { Timeout = 10_000 });
        await back.ClickAsync();

        await Page.WaitForURLAsync(new Regex(@"/captures$"), new PageWaitForURLOptions { Timeout = 15_000 });
    }
}

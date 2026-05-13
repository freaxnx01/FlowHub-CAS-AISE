using System.Text.RegularExpressions;

namespace FlowHub.Web.E2ETests.Journeys;

[Trait("Category", "E2E")]
[Trait("Journey", "J24")]
[Collection("Playwright")]
public sealed class J24_NewCaptureCancelReturnsToDashboard : JourneyTestBase
{
    public J24_NewCaptureCancelReturnsToDashboard(PlaywrightFixture fixture) : base(fixture, "J24.json") { }

    [Fact]
    public async Task ClickCancel_NavigatesToRoot()
    {
        await GotoAsync("/captures/new");

        var cancel = Page.GetByRole(AriaRole.Button, new() { Name = "Cancel" }).First;
        await cancel.WaitForAsync(new LocatorWaitForOptions { Timeout = 15_000 });
        await cancel.ClickAsync();

        await Page.WaitForURLAsync(new Regex(@"/$"), new PageWaitForURLOptions { Timeout = 15_000 });
    }
}

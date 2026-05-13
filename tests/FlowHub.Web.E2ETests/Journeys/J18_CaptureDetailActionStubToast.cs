namespace FlowHub.Web.E2ETests.Journeys;

[Trait("Category", "E2E")]
[Trait("Journey", "J18")]
[Collection("Playwright")]
public sealed class J18_CaptureDetailActionStubToast : JourneyTestBase
{
    public J18_CaptureDetailActionStubToast(PlaywrightFixture fixture) : base(fixture, "J18.json") { }

    [Fact]
    public async Task ClickIgnoreOnOrphanOrUnhandled_ShowsBlockTwoStubSnackbar()
    {
        await GotoAsync("/captures");

        // Prefer Orphan; fall back to Unhandled
        var row = Page.Locator(".mud-table-row", new PageLocatorOptions { HasText = "orphan" }).First;
        if (await row.CountAsync() == 0)
        {
            row = Page.Locator(".mud-table-row", new PageLocatorOptions { HasText = "unhandled" }).First;
        }
        await row.WaitForAsync(new LocatorWaitForOptions { Timeout = 15_000 });
        await row.ClickAsync();

        var ignoreBtn = Page.GetByRole(AriaRole.Button, new() { Name = "Ignore" }).First;
        await ignoreBtn.WaitForAsync(new LocatorWaitForOptions { Timeout = 15_000 });
        await ignoreBtn.ClickAsync();

        await Page.GetByText("will work once backend Skills are wired", new PageGetByTextOptions { Exact = false })
            .First.WaitForAsync(new LocatorWaitForOptions { Timeout = 10_000 });
    }
}

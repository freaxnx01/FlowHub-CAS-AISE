namespace FlowHub.Web.E2ETests.Journeys;

[Trait("Category", "E2E")]
[Trait("Journey", "J17")]
[Collection("Playwright")]
public sealed class J17_CaptureDetailUnhandledActions : JourneyTestBase
{
    public J17_CaptureDetailUnhandledActions(PlaywrightFixture fixture) : base(fixture, "J17.json") { }

    [Fact]
    public async Task OpenUnhandledCapture_ShowsAssignActionButNoRetry()
    {
        await GotoAsync("/captures");

        var row = Page.Locator(".mud-table-row", new PageLocatorOptions { HasText = "unhandled" }).First;
        await row.WaitForAsync(new LocatorWaitForOptions { Timeout = 15_000 });
        await row.ClickAsync();

        await Page.Locator(".mud-alert", new PageLocatorOptions { HasText = "Skill integration failed" })
            .First.WaitForAsync(new LocatorWaitForOptions { Timeout = 15_000 });

        (await Page.GetByText("Assign skill").CountAsync()).Should().BeGreaterThan(0);
        (await Page.GetByRole(AriaRole.Button, new() { Name = "Retry routing" }).CountAsync()).Should().Be(0);
    }
}

namespace FlowHub.Web.E2ETests.Journeys;

[Trait("Category", "E2E")]
[Trait("Journey", "J16")]
[Collection("Playwright")]
public sealed class J16_CaptureDetailOrphanActions : JourneyTestBase
{
    public J16_CaptureDetailOrphanActions(PlaywrightFixture fixture) : base(fixture, "J16.json") { }

    [Fact]
    public async Task OpenOrphanCapture_ShowsFailureAlertAndAllActions()
    {
        await GotoAsync("/captures");

        var row = Page.Locator(".mud-table-row", new PageLocatorOptions { HasText = "orphan" }).First;
        await row.WaitForAsync(new LocatorWaitForOptions { Timeout = 15_000 });
        await row.ClickAsync();

        await Page.Locator(".mud-alert", new PageLocatorOptions { HasText = "Routing failed" })
            .First.WaitForAsync(new LocatorWaitForOptions { Timeout = 15_000 });

        await Page.GetByRole(AriaRole.Button, new() { Name = "Retry routing" })
            .First.WaitForAsync(new LocatorWaitForOptions { Timeout = 5_000 });
        (await Page.GetByText("Reassign skill").CountAsync()).Should().BeGreaterThan(0);
        (await Page.GetByText("Ignore").CountAsync()).Should().BeGreaterThan(0);
    }
}

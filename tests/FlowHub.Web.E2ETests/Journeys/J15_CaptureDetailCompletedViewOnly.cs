namespace FlowHub.Web.E2ETests.Journeys;

[Trait("Category", "E2E")]
[Trait("Journey", "J15")]
[Collection("Playwright")]
public sealed class J15_CaptureDetailCompletedViewOnly : JourneyTestBase
{
    public J15_CaptureDetailCompletedViewOnly(PlaywrightFixture fixture) : base(fixture, "J15.json") { }

    [Fact]
    public async Task OpenCompletedCapture_ShowsMetadataWithoutActionButtons()
    {
        await GotoAsync("/captures");

        // Find a row whose lifecycle badge reads 'completed' and click it
        var row = Page.Locator(".mud-table-row", new PageLocatorOptions { HasText = "completed" }).First;
        await row.WaitForAsync(new LocatorWaitForOptions { Timeout = 15_000 });
        await row.ClickAsync();

        await Page.GetByText("Metadata", new PageGetByTextOptions { Exact = false })
            .First.WaitForAsync(new LocatorWaitForOptions { Timeout = 15_000 });

        (await Page.GetByText("Routing failed").CountAsync()).Should().Be(0);
        (await Page.GetByRole(AriaRole.Button, new() { Name = "Retry routing" }).CountAsync()).Should().Be(0);
    }
}

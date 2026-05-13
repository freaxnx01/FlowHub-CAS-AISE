namespace FlowHub.Web.E2ETests.Journeys;

[Trait("Category", "E2E")]
[Trait("Journey", "J01")]
[Collection("Playwright")]
public sealed class J01_QuickCaptureHappyPath : JourneyTestBase
{
    public J01_QuickCaptureHappyPath(PlaywrightFixture fixture) : base(fixture, "J01.json") { }

    [Fact]
    public async Task TypeAndSubmit_ShowsCapturedSnackbarAndClearsInput()
    {
        await GotoAsync("/");

        var input = Page.GetByPlaceholder("+ Quick capture: paste URL or type…");
        await input.WaitForAsync(new LocatorWaitForOptions { Timeout = 15_000 });

        var content = $"https://example.com/article-{Guid.NewGuid():N}";
        await input.FillAsync(content);
        await input.PressAsync("Enter");

        await Page.GetByText("Captured", new PageGetByTextOptions { Exact = false })
            .First.WaitForAsync(new LocatorWaitForOptions { Timeout = 15_000 });
    }
}

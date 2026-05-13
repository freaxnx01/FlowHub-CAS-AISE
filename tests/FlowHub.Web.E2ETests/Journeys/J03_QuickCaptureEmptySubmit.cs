namespace FlowHub.Web.E2ETests.Journeys;

[Trait("Category", "E2E")]
[Trait("Journey", "J03")]
[Collection("Playwright")]
public sealed class J03_QuickCaptureEmptySubmit : JourneyTestBase
{
    public J03_QuickCaptureEmptySubmit(PlaywrightFixture fixture) : base(fixture, "J03.json") { }

    [Fact]
    public async Task EmptyEnter_ShowsTypeSomethingFirstSnackbar()
    {
        await GotoAsync("/");

        var input = Page.GetByPlaceholder("+ Quick capture: paste URL or type…");
        await input.WaitForAsync(new LocatorWaitForOptions { Timeout = 15_000 });
        await input.FocusAsync();
        await input.PressAsync("Enter");

        await Page.GetByText("Type something first", new PageGetByTextOptions { Exact = false })
            .First.WaitForAsync(new LocatorWaitForOptions { Timeout = 10_000 });
    }
}

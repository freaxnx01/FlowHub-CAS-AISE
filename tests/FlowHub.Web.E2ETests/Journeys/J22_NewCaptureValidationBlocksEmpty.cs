namespace FlowHub.Web.E2ETests.Journeys;

[Trait("Category", "E2E")]
[Trait("Journey", "J22")]
[Collection("Playwright")]
public sealed class J22_NewCaptureValidationBlocksEmpty : JourneyTestBase
{
    public J22_NewCaptureValidationBlocksEmpty(PlaywrightFixture fixture) : base(fixture, "J22.json") { }

    [Fact]
    public async Task SubmitEmpty_ShowsRequiredValidationError()
    {
        await GotoAsync("/captures/new");

        var submit = Page.GetByRole(AriaRole.Button, new() { Name = "Submit" }).First;
        await submit.WaitForAsync(new LocatorWaitForOptions { Timeout = 15_000 });
        await submit.ClickAsync();

        await Page.GetByText("Content is required", new PageGetByTextOptions { Exact = false })
            .First.WaitForAsync(new LocatorWaitForOptions { Timeout = 10_000 });
    }
}

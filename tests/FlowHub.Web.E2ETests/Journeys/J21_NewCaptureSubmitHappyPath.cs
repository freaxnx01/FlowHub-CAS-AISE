namespace FlowHub.Web.E2ETests.Journeys;

[Trait("Category", "E2E")]
[Trait("Journey", "J21")]
[Collection("Playwright")]
public sealed class J21_NewCaptureSubmitHappyPath : JourneyTestBase
{
    public J21_NewCaptureSubmitHappyPath(PlaywrightFixture fixture) : base(fixture, "J21.json") { }

    [Fact]
    public async Task FillContentAndSubmit_ShowsCapturedSnackbar()
    {
        await GotoAsync("/captures/new");

        var content = $"Read this article tomorrow {Guid.NewGuid():N}";
        var contentField = Page.GetByLabel("Content").First;
        await contentField.WaitForAsync(new LocatorWaitForOptions { Timeout = 15_000 });
        await contentField.FillAsync(content);

        var submit = Page.GetByRole(AriaRole.Button, new() { Name = "Submit" }).First;
        await submit.ClickAsync();

        await Page.GetByText("Captured", new PageGetByTextOptions { Exact = false })
            .First.WaitForAsync(new LocatorWaitForOptions { Timeout = 15_000 });
    }
}

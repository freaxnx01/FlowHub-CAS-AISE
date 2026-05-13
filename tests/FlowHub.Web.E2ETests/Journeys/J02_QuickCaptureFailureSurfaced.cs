namespace FlowHub.Web.E2ETests.Journeys;

[Trait("Category", "E2E")]
[Trait("Journey", "J02")]
[Collection("Playwright")]
public sealed class J02_QuickCaptureFailureSurfaced : JourneyTestBase
{
    public J02_QuickCaptureFailureSurfaced(PlaywrightFixture fixture) : base(fixture, "J02.json") { }

    [Fact]
    public async Task TypeAndSubmit_AgainstHealthySystem_ShowsCapturedOrFailedToast()
    {
        // Live system can't be forced to throw without DI surgery. The bUnit suite
        // (QuickCaptureFieldTests.Submit_ServiceThrows_DoesNotCrashComponent) covers
        // the negative path. This E2E spec exists for completeness — it asserts that
        // SOME snackbar surfaces, regardless of healthy/failure path.
        await GotoAsync("/");

        var input = Page.GetByPlaceholder("+ Quick capture: paste URL or type…");
        await input.WaitForAsync(new LocatorWaitForOptions { Timeout = 15_000 });
        await input.FillAsync("foo");
        await input.PressAsync("Enter");

        await Page.Locator(".mud-snackbar").First
            .WaitForAsync(new LocatorWaitForOptions { Timeout = 15_000 });
    }
}

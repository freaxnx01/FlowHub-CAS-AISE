namespace FlowHub.Web.E2ETests.Journeys;

[Trait("Category", "E2E")]
[Trait("Journey", "J23")]
[Collection("Playwright")]
public sealed class J23_NewCaptureSkillsLoadFailure : JourneyTestBase
{
    public J23_NewCaptureSkillsLoadFailure(PlaywrightFixture fixture) : base(fixture, "J23.json") { }

    [Fact]
    public async Task SubmitSucceedsEvenIfSkillsWarningIsShown()
    {
        // Cannot force the skills failure path against live persistence. bUnit
        // (Pages/SkillsTests / NewCaptureTests) covers the actual error branch.
        // This spec asserts that the form is reachable + submits cleanly.
        await GotoAsync("/captures/new");

        var contentField = Page.GetByLabel("Content").First;
        await contentField.WaitForAsync(new LocatorWaitForOptions { Timeout = 15_000 });
        await contentField.FillAsync($"still works {Guid.NewGuid():N}");

        await Page.GetByRole(AriaRole.Button, new() { Name = "Submit" }).First.ClickAsync();

        await Page.GetByText("Captured", new PageGetByTextOptions { Exact = false })
            .First.WaitForAsync(new LocatorWaitForOptions { Timeout = 15_000 });
    }
}

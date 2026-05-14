namespace FlowHub.Web.E2ETests.Journeys;

[Trait("Category", "E2E")]
[Trait("Journey", "J15")]
[Collection("Playwright")]
public sealed class J15_CaptureDetailCompletedViewOnly : JourneyTestBase
{
    public J15_CaptureDetailCompletedViewOnly(PlaywrightFixture fixture) : base(fixture, "J15.json") { }

    // Stable Guid for the J15 fixture row. Idempotent upsert means concurrent
    // runs and re-runs are safe.
    private static readonly Guid CompletedFixtureId =
        Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public async Task OpenCompletedCapture_ShowsMetadataWithoutActionButtons()
    {
        await E2EDbHelpers.UpsertCompletedCaptureAsync(
            CompletedFixtureId,
            "J15 fixture — Completed capture, view-only");

        await GotoAsync($"/captures/{CompletedFixtureId}");

        await Page.GetByText("Metadata", new PageGetByTextOptions { Exact = false })
            .First.WaitForAsync(new LocatorWaitForOptions { Timeout = 15_000 });

        (await Page.GetByText("Routing failed").CountAsync()).Should().Be(0);
        (await Page.GetByRole(AriaRole.Button, new() { Name = "Retry routing" }).CountAsync()).Should().Be(0);
    }
}

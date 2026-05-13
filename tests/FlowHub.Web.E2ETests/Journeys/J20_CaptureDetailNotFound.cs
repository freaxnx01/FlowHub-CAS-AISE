namespace FlowHub.Web.E2ETests.Journeys;

/// <summary>
/// J20 — see Journeys/J20.json (loaded at runtime).
/// Pattern: keep the user-facing acceptance criteria in the JSON file; the
/// C# spec is the executable encoding of those steps and assertions.
/// </summary>
[Trait("Category", "E2E")]
[Trait("Journey", "J20")]
[Collection("Playwright")]
public sealed class J20_CaptureDetailNotFound
{
    private readonly PlaywrightFixture _fixture;
    private readonly JourneyAcceptance _ac = JourneyAcceptance.Load("J20.json");

    public J20_CaptureDetailNotFound(PlaywrightFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task UnknownCaptureId_ShowsNotFoundAlert()
    {
        _ac.Id.Should().Be("J20");
        _ac.Category.Should().Be("edge");

        await using var context = await _fixture.Browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = _fixture.BaseUrl,
            ViewportSize = new ViewportSize { Width = 1280, Height = 800 },
        });
        var page = await context.NewPageAsync();

        // NetworkIdle is unreliable under Interactive Server Blazor — the SignalR
        // circuit keeps the network busy. Wait for DOMContentLoaded then poll the
        // alert via a stable selector that survives the SSR → interactive handoff.
        await page.GotoAsync(_ac.EntryUrl, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        await page.Locator(".mud-alert", new PageLocatorOptions { HasText = "Capture not found" })
            .First.WaitForAsync(new LocatorWaitForOptions { Timeout = 15_000 });
    }
}

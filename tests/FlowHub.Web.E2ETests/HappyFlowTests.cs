namespace FlowHub.Web.E2ETests;

[Trait("Category", "E2E")]
[Collection("Playwright")]
public sealed class HappyFlowTests
{
    private readonly PlaywrightFixture _fixture;

    public HappyFlowTests(PlaywrightFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task QuickCapture_TodoEntry_AppearsInCapturesListAndDetail()
    {
        await using var context = await _fixture.Browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = _fixture.BaseUrl,
            ViewportSize = new ViewportSize { Width = 1280, Height = 800 },
        });
        var page = await context.NewPageAsync();

        var content = $"todo: e2e happy-flow {Guid.NewGuid():N}";

        await page.GotoAsync("/", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

        var quickCapture = page.GetByPlaceholder("+ Quick capture: paste URL or type…");
        await quickCapture.WaitForAsync(new LocatorWaitForOptions { Timeout = 15_000 });
        await quickCapture.FillAsync(content);
        await quickCapture.PressAsync("Enter");

        // Success snackbar from QuickCaptureField.SubmitAsync
        await page.GetByText("Captured", new PageGetByTextOptions { Exact = false })
            .First.WaitForAsync(new LocatorWaitForOptions { Timeout = 15_000 });

        await page.GotoAsync("/captures", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

        var row = page.Locator("tr", new PageLocatorOptions { HasText = content });
        await row.WaitForAsync(new LocatorWaitForOptions { Timeout = 15_000 });

        await row.ClickAsync();

        await page.WaitForURLAsync(new System.Text.RegularExpressions.Regex(@"/captures/[0-9a-fA-F-]{36}$"),
            new PageWaitForURLOptions { Timeout = 15_000 });

        await page.GetByText(content).First.WaitForAsync(new LocatorWaitForOptions { Timeout = 10_000 });
    }
}

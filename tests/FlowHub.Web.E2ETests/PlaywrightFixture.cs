namespace FlowHub.Web.E2ETests;

public sealed class PlaywrightFixture : IAsyncLifetime
{
    public IPlaywright Playwright { get; private set; } = default!;
    public IBrowser Browser { get; private set; } = default!;

    public string BaseUrl =>
        Environment.GetEnvironmentVariable("FLOWHUB_E2E_BASE_URL")
        ?? "http://localhost:5070";

    public async Task InitializeAsync()
    {
        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        var headless = !string.Equals(
            Environment.GetEnvironmentVariable("FLOWHUB_E2E_HEADED"),
            "true",
            StringComparison.OrdinalIgnoreCase);
        Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = headless,
        });
    }

    public async Task DisposeAsync()
    {
        await Browser.CloseAsync();
        Playwright.Dispose();
    }
}

[CollectionDefinition("Playwright")]
public sealed class PlaywrightCollection : ICollectionFixture<PlaywrightFixture>;

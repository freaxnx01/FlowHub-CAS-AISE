namespace FlowHub.Web.E2ETests.Journeys;

/// <summary>
/// Per-test browser context + JSON acceptance loader. Each [Fact] gets a
/// fresh context (xUnit instantiates the class per test). Spec subclasses
/// just write the steps; setup/teardown lives here.
/// </summary>
public abstract class JourneyTestBase : IAsyncLifetime
{
    protected PlaywrightFixture Fixture { get; }
    protected JourneyAcceptance Ac { get; }
    protected IBrowserContext Context { get; private set; } = null!;
    protected IPage Page { get; private set; } = null!;

    protected JourneyTestBase(PlaywrightFixture fixture, string jsonFileName)
    {
        Fixture = fixture;
        Ac = JourneyAcceptance.Load(jsonFileName);
    }

    public async Task InitializeAsync()
    {
        Context = await Fixture.Browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = Fixture.BaseUrl,
            ViewportSize = new ViewportSize { Width = 1280, Height = 800 },
        });
        Page = await Context.NewPageAsync();
    }

    public async Task DisposeAsync()
    {
        await Context.DisposeAsync();
    }

    /// <summary>
    /// Stable navigation for Interactive Server Blazor: DOMContentLoaded for the
    /// shell, then a brief warmup so the SignalR circuit can wire up event
    /// handlers (RowClick, OnAdornmentClick, button OnClick, …). Without the
    /// warmup, fast Playwright clicks fire before Blazor binds the handlers.
    /// </summary>
    protected async Task GotoAsync(string url)
    {
        await Page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await Page.WaitForTimeoutAsync(2000);
    }
}

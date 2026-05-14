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
    /// Stable navigation for Interactive Server Blazor:
    /// 1. DOMContentLoaded for the shell.
    /// 2. #blazor-circuit-ready sentinel — MainLayout renders this only after
    ///    OnAfterRender(firstRender), proving the SignalR circuit is connected.
    /// 3. All MudSkeleton placeholders gone — proves the page's OnInitializedAsync
    ///    data load completed and the resulting interactive DOM (with bound event
    ///    handlers like MudDataGrid.RowClick) has been rendered.
    /// </summary>
    protected async Task GotoAsync(string url)
    {
        await Page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await Page.Locator("#blazor-circuit-ready")
            .WaitForAsync(new LocatorWaitForOptions
            {
                Timeout = 15_000,
                State = WaitForSelectorState.Attached,
            });
        // Best-effort: some pages keep a permanent skeleton when a backing service
        // (e.g. integration health) is slow or empty. Wait briefly and move on.
        try
        {
            await Page.WaitForFunctionAsync(
                "() => document.querySelectorAll('.mud-skeleton').length === 0",
                null,
                new PageWaitForFunctionOptions { Timeout = 3_000 });
        }
        catch (TimeoutException)
        {
        }
    }
}

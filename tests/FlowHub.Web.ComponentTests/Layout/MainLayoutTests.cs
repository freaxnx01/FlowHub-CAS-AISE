using FlowHub.Web.Components.Layout;
using FlowHub.Web.Uploads;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

namespace FlowHub.Web.ComponentTests.Layout;

public class MainLayoutTests : TestContext
{
    private readonly ConfigurationManager _config = new();

    public MainLayoutTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;

        // QuickCaptureField is rendered by the layout; register its deps.
        Services.AddSingleton(Substitute.For<ICaptureService>());
        var policy = Substitute.For<IUploadPolicy>();
        policy.MaxBytes.Returns(2_097_152L);
        policy.AllowedContentTypes.Returns(new[] { "application/pdf" });
        policy.AcceptAttribute.Returns(string.Empty);
        Services.AddSingleton(policy);

        // ConfigurationManager is mutable, so each test can set Demo:* keys
        // *before* MainLayout renders without re-registering the service.
        Services.AddSingleton<IConfiguration>(_config);

        // No pre-render of MudPopoverProvider — MainLayout already renders
        // one itself; rendering a second triggers a duplicate section-outlet
        // subscriber error.
    }

    private void UseConfig(Dictionary<string, string?>? settings = null)
    {
        if (settings is null) return;
        foreach (var (k, v) in settings) _config[k] = v;
    }

    [Fact]
    public void Render_WithDefaultConfig_ShowsAppBarAndNavLinks()
    {
        UseConfig();

        var cut = RenderComponent<MainLayout>();

        cut.Markup.Should().Contain("FlowHub"); // logo alt + page title
        // Nav links — Dashboard, Captures, New Capture, Skills, Integrations
        cut.Markup.Should().Contain("Dashboard");
        cut.Markup.Should().Contain("Captures");
        cut.Markup.Should().Contain("New Capture");
        cut.Markup.Should().Contain("Skills");
        cut.Markup.Should().Contain("Integrations");
        // QuickCaptureField is hosted in the app bar.
        cut.Markup.Should().Contain("Quick capture");
    }

    [Fact]
    public void Render_DemoModeOff_DoesNotShowDemoBanner()
    {
        UseConfig(new Dictionary<string, string?> { ["Demo:Mode"] = "false" });

        var cut = RenderComponent<MainLayout>();

        cut.Markup.Should().NotContain("Public demo");
        // No example chips either — DemoMode flows into QuickCaptureField.
        cut.Markup.Should().NotContain("Try:");
    }

    [Fact]
    public void Render_DemoModeOn_ShowsDemoBannerWithDefaultText_AndExampleChips()
    {
        UseConfig(new Dictionary<string, string?> { ["Demo:Mode"] = "true" });

        var cut = RenderComponent<MainLayout>();

        // Default banner text fallback when Demo:BannerText is not set.
        cut.Markup.Should().Contain("Public demo");
        cut.Markup.Should().Contain("data resets every 15 minutes");
        // Demo mode also flips QuickCaptureField into chip mode.
        cut.Markup.Should().Contain("Try:");
    }

    [Fact]
    public void Render_DemoModeOn_IsCaseInsensitive()
    {
        UseConfig(new Dictionary<string, string?> { ["Demo:Mode"] = "TRUE" });

        var cut = RenderComponent<MainLayout>();

        cut.Markup.Should().Contain("Public demo");
    }

    [Fact]
    public void Render_DemoModeOn_PassesAllDemoUrlsToBanner()
    {
        UseConfig(new Dictionary<string, string?>
        {
            ["Demo:Mode"] = "true",
            ["Demo:BannerText"] = "Custom demo banner",
            ["Demo:RepoUrl"] = "https://github.com/example/repo",
            ["Demo:WalkthroughUrl"] = "https://example.com/walk",
            ["Demo:StatusUrl"] = "https://status.example.com/x",
            ["Demo:Vikunja:ShareUrl"] = "https://vikunja.example.com/share/skills",
            ["Demo:Vikunja:ZitateShareUrl"] = "https://vikunja.example.com/share/zitate",
            ["Demo:Wallabag:Url"] = "https://wallabag.example.com",
            ["Demo:Paperless:Url"] = "https://paperless.example.com",
            ["Demo:ServiceLogin"] = "flowhub / flowhub-demo",
        });

        var cut = RenderComponent<MainLayout>();

        cut.Markup.Should().Contain("Custom demo banner");
        cut.Markup.Should().Contain("https://github.com/example/repo");
        cut.Markup.Should().Contain("https://example.com/walk");
        cut.Markup.Should().Contain("https://status.example.com/x");
        cut.Markup.Should().Contain("https://vikunja.example.com/share/skills");
        cut.Markup.Should().Contain("https://vikunja.example.com/share/zitate");
        cut.Markup.Should().Contain("https://wallabag.example.com");
        cut.Markup.Should().Contain("https://paperless.example.com");
        cut.Markup.Should().Contain("flowhub / flowhub-demo");
    }

    [Fact]
    public void Render_AfterFirstRender_EmitsCircuitReadySentinel()
    {
        UseConfig();

        var cut = RenderComponent<MainLayout>();

        // OnAfterRender(firstRender:true) flips _circuitReady and calls StateHasChanged,
        // so the E2E sentinel must be present after the initial render cycle.
        cut.WaitForAssertion(() =>
            cut.Find("#blazor-circuit-ready").Should().NotBeNull());
    }

    [Fact]
    public void ToggleDrawer_ClickingMenuButton_FlipsDrawerOpenState()
    {
        UseConfig();

        var cut = RenderComponent<MainLayout>();
        var menuButton = cut.Find(".mud-appbar .mud-icon-button");
        var drawer = cut.Find(".mud-drawer");

        // Mini drawer renders in both states; the open/closed delta is the
        // "mud-drawer--open" modifier class on the drawer root.
        var initiallyOpen = drawer.ClassList.Contains("mud-drawer--open");

        menuButton.Click();
        cut.WaitForAssertion(() =>
            cut.Find(".mud-drawer").ClassList.Contains("mud-drawer--open")
                .Should().Be(!initiallyOpen));

        menuButton.Click();
        cut.WaitForAssertion(() =>
            cut.Find(".mud-drawer").ClassList.Contains("mud-drawer--open")
                .Should().Be(initiallyOpen));
    }
}

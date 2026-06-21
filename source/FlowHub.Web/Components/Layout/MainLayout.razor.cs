using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FlowHub.Web.Components.Layout;

public partial class MainLayout : LayoutComponentBase
{
    [Inject] private IConfiguration Configuration { get; set; } = default!;

    private bool _drawerOpen;
    private bool _circuitReady;

    // The app bar hosts the Quick-Capture field and the demo "Try:" chips, all of
    // which use Color.Inherit. Against MudBlazor's default near-white light-mode
    // app bar, the orange logo, the outlined text field's border, and the chips all
    // wash out. Pinning the light-mode app bar to the dark-mode tone gives them the
    // same legible contrast they already have in dark mode; the rest of the page
    // keeps the default light palette.
    private readonly MudTheme _theme = new()
    {
        PaletteLight = new PaletteLight
        {
            AppbarBackground = "#1a1a27",
            AppbarText = "#ffffffeb",
        },
    };

    private void ToggleDrawer() => _drawerOpen = !_drawerOpen;

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender && !_circuitReady)
        {
            _circuitReady = true;
            StateHasChanged();
        }
    }

    private bool DemoMode => string.Equals(
        Configuration["Demo:Mode"],
        "true",
        System.StringComparison.OrdinalIgnoreCase);

    private string DemoBannerText => Configuration["Demo:BannerText"]
        ?? "Public demo — data resets every 15 minutes. Skill writes + embeddings disabled.";

    private string? DemoRepoUrl => Configuration["Demo:RepoUrl"];

    private string? DemoWalkthroughUrl => Configuration["Demo:WalkthroughUrl"];

    private string? DemoStatusUrl => Configuration["Demo:StatusUrl"];

    private string? DemoSkillBoardUrl => Configuration["Demo:Vikunja:ShareUrl"];

    private string? DemoZitateBoardUrl => Configuration["Demo:Vikunja:ZitateShareUrl"];

    private string? DemoWallabagUrl => Configuration["Demo:Wallabag:Url"];

    private string? DemoPaperlessUrl => Configuration["Demo:Paperless:Url"];

    private string? DemoServiceLogin => Configuration["Demo:ServiceLogin"];
}

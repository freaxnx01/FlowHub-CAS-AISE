using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;

namespace FlowHub.Web.Components.Layout;

public partial class MainLayout : LayoutComponentBase
{
    [Inject] private IConfiguration Configuration { get; set; } = default!;

    private bool _drawerOpen;
    private bool _circuitReady;

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

    private string? DemoSkillBoardUrl => Configuration["Demo:Vikunja:ShareUrl"];
}

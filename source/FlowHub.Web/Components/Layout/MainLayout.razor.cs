using Microsoft.AspNetCore.Components;

namespace FlowHub.Web.Components.Layout;

public partial class MainLayout : LayoutComponentBase
{
    private bool _drawerOpen;

    private void ToggleDrawer() => _drawerOpen = !_drawerOpen;
}

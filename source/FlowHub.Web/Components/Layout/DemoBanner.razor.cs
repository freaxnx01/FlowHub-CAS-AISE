using Microsoft.AspNetCore.Components;

namespace FlowHub.Web.Components.Layout;

/// <summary>
/// Demo-mode info banner: the configured banner text plus an optional clickable
/// "Source on GitHub" link to the repository. Presentational — driven by
/// <c>Demo:BannerText</c> and <c>Demo:RepoUrl</c> via <see cref="MainLayout"/>.
/// </summary>
public partial class DemoBanner : ComponentBase
{
    [Parameter] public string? BannerText { get; set; }

    [Parameter] public string? RepoUrl { get; set; }
}

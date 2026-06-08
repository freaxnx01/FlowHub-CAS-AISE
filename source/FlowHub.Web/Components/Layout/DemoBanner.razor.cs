using Microsoft.AspNetCore.Components;

namespace FlowHub.Web.Components.Layout;

/// <summary>
/// Demo-mode info banner: the configured banner text plus optional clickable
/// links — "Source on GitHub" and, when the demo routes to a live Vikunja board,
/// a "View routed tasks" link to its public share. Presentational — driven by
/// <c>Demo:BannerText</c>, <c>Demo:RepoUrl</c> and <c>Demo:Vikunja:ShareUrl</c>
/// via <see cref="MainLayout"/>.
/// </summary>
public partial class DemoBanner : ComponentBase
{
    [Parameter] public string? BannerText { get; set; }

    [Parameter] public string? RepoUrl { get; set; }

    [Parameter] public string? SkillBoardUrl { get; set; }
}

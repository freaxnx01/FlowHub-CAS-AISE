using Microsoft.AspNetCore.Components;

namespace FlowHub.Web.Components.Layout;

/// <summary>
/// Demo-mode info banner: the configured banner text plus optional clickable
/// quick-links to the live downstream services — Vikunja (public share),
/// Wallabag and paperless-ngx — and "Source on GitHub". Wallabag and paperless
/// sit behind login, so a shared <see cref="ServiceLogin"/> hint is shown when
/// either of their links is present. Presentational — driven by
/// <c>Demo:BannerText</c>, <c>Demo:RepoUrl</c>, <c>Demo:Vikunja:ShareUrl</c>,
/// <c>Demo:Wallabag:Url</c>, <c>Demo:Paperless:Url</c> and
/// <c>Demo:ServiceLogin</c> via <see cref="MainLayout"/>.
/// </summary>
public partial class DemoBanner : ComponentBase
{
    [Parameter] public string? BannerText { get; set; }

    [Parameter] public string? RepoUrl { get; set; }

    [Parameter] public string? SkillBoardUrl { get; set; }

    [Parameter] public string? WallabagUrl { get; set; }

    [Parameter] public string? PaperlessUrl { get; set; }

    /// <summary>Shared demo login for the login-gated services (Wallabag, paperless).</summary>
    [Parameter] public string? ServiceLogin { get; set; }

    private bool HasLoginGatedServiceLink =>
        !string.IsNullOrWhiteSpace(WallabagUrl) || !string.IsNullOrWhiteSpace(PaperlessUrl);
}

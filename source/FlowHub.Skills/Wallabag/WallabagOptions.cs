namespace FlowHub.Skills.Wallabag;

/// <summary>
/// Bound from configuration section <c>Skills:Wallabag</c>.
/// Wallabag's API is OAuth2 (password grant); access tokens expire (~1h), so the
/// skill obtains and refreshes its own token from these credentials.
/// </summary>
public sealed class WallabagOptions
{
    public const string SectionName = "Skills:Wallabag";

    public string? BaseUrl { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
}

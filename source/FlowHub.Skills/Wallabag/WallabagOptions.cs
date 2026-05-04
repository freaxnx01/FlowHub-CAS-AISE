namespace FlowHub.Skills.Wallabag;

/// <summary>
/// Bound from configuration section <c>Skills:Wallabag</c>.
/// </summary>
public sealed class WallabagOptions
{
    public const string SectionName = "Skills:Wallabag";

    public string? BaseUrl { get; set; }
    public string? ApiToken { get; set; }
}

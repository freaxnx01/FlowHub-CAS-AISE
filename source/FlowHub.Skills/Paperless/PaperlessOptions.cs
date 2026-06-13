namespace FlowHub.Skills.Paperless;

/// <summary>
/// Bound from configuration section <c>Skills:Paperless</c>.
/// </summary>
public sealed class PaperlessOptions
{
    public const string SectionName = "Skills:Paperless";

    public string? BaseUrl { get; set; }
    public string? ApiToken { get; set; }
}

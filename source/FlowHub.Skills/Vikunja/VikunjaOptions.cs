namespace FlowHub.Skills.Vikunja;

/// <summary>
/// Bound from configuration section <c>Skills:Vikunja</c>.
/// </summary>
public sealed class VikunjaOptions
{
    public const string SectionName = "Skills:Vikunja";

    public string? BaseUrl { get; set; }
    public string? ApiToken { get; set; }
    public int DefaultProjectId { get; set; }
}

namespace FlowHub.Skills.Vikunja;

/// <summary>
/// Bound from configuration section <c>Skills:Vikunja</c>.
/// </summary>
public sealed class VikunjaOptions
{
    public const string SectionName = "Skills:Vikunja";

    public string? BaseUrl { get; set; }
    public string? ApiToken { get; set; }

    /// <summary>Bucket name used when the classifier returns an unknown project
    /// or before the catalog has been fetched.</summary>
    public string FallbackProject { get; set; } = "Inbox";

    /// <summary>Project id used until the first successful catalog fetch.</summary>
    public int FallbackProjectId { get; set; }

    public VikunjaCatalogOptions Catalog { get; set; } = new();
}

public sealed class VikunjaCatalogOptions
{
    public TimeSpan RefreshInterval { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(3);
}

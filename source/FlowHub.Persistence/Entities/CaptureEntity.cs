namespace FlowHub.Persistence.Entities;

/// <summary>
/// EF Core persistence shape of <see cref="FlowHub.Core.Captures.Capture"/>.
/// Strings (not enums) are stored for <see cref="Source"/> and <see cref="Stage"/>
/// to keep SQLite columns human-inspectable.
/// </summary>
public sealed class CaptureEntity
{
    public Guid Id { get; set; }
    public string Content { get; set; } = "";
    public string Source { get; set; } = "";
    public string Stage { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public string? MatchedSkill { get; set; }
    public string? Title { get; set; }
    public string? FailureReason { get; set; }
    public string? ExternalRef { get; set; }
}

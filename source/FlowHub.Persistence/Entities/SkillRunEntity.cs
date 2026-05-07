namespace FlowHub.Persistence.Entities;

internal sealed class SkillRunEntity
{
    public Guid Id { get; set; }
    public string SkillName { get; set; } = "";
    public Guid CaptureId { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public bool Success { get; set; }
    public string? FailureReason { get; set; }
    public SkillEntity Skill { get; set; } = null!;
    public CaptureEntity Capture { get; set; } = null!;
}

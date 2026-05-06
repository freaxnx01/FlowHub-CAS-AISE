namespace FlowHub.Core.Captures;

public sealed record SkillRun(
    Guid Id,
    string SkillName,
    Guid CaptureId,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    bool Success,
    string? FailureReason);

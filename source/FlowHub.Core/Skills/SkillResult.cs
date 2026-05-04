namespace FlowHub.Core.Skills;

/// <summary>
/// Outcome of <see cref="ISkillIntegration.HandleAsync"/>.
/// <see cref="ExternalRef"/> is the downstream system's identifier (Wallabag entry id,
/// Vikunja task id) and is persisted on the Capture for later click-through.
/// <see cref="FailureReason"/> is only meaningful when <see cref="Success"/> is false.
/// </summary>
public sealed record SkillResult(
    bool Success,
    string? ExternalRef = null,
    string? FailureReason = null);

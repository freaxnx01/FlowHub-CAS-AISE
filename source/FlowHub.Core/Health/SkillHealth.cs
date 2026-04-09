namespace FlowHub.Core.Health;

/// <summary>
/// Snapshot of a Skill's recent activity and status.
/// </summary>
public sealed record SkillHealth(
    string Name,
    HealthStatus Status,
    int RoutedToday);

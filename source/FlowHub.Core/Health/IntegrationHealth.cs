namespace FlowHub.Core.Health;

/// <summary>
/// Snapshot of a downstream Integration's reachability and recent activity.
/// </summary>
public sealed record IntegrationHealth(
    string Name,
    HealthStatus Status,
    DateTimeOffset? LastWriteAt,
    TimeSpan? LastWriteDuration);

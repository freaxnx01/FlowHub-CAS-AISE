namespace FlowHub.Core.Health;

/// <summary>
/// Coarse-grained health indicator for a Skill or Integration.
/// </summary>
public enum HealthStatus
{
    Unknown,
    Healthy,
    Degraded,
    Down,
}

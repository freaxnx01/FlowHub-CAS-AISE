namespace FlowHub.Core.Health;

/// <summary>
/// Driving port for querying Skill registration and current health.
/// </summary>
public interface ISkillRegistry
{
    Task<IReadOnlyList<SkillHealth>> GetHealthAsync(CancellationToken cancellationToken = default);
}

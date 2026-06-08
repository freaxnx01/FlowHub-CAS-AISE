namespace FlowHub.Core.Health;

/// <summary>
/// Driven port for skill registry health (<see cref="SkillHealth"/>) shown on the
/// dashboard. EF Core implementation in <c>FlowHub.Persistence</c>.
/// </summary>
public interface ISkillRepository
{
    /// <summary>Returns health/status for all registered skills.</summary>
    Task<IReadOnlyList<SkillHealth>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns health for a single skill, or <c>null</c> if unknown.</summary>
    Task<SkillHealth?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>Inserts or updates a skill's health record (keyed by name).</summary>
    Task UpsertAsync(SkillHealth skill, CancellationToken cancellationToken = default);

    /// <summary>Increments the "routed today" counter for the given skill.</summary>
    Task IncrementRoutedTodayAsync(string name, CancellationToken cancellationToken = default);
}

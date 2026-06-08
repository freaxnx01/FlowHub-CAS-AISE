namespace FlowHub.Core.Captures;

/// <summary>
/// Driven port for the audit trail of skill executions (<see cref="SkillRun"/>)
/// against captures. EF Core implementation in <c>FlowHub.Persistence</c>.
/// </summary>
public interface ISkillRunRepository
{
    /// <summary>Records a skill run and returns the stored instance.</summary>
    Task<SkillRun> AddAsync(SkillRun skillRun, CancellationToken cancellationToken = default);

    /// <summary>Returns all skill runs recorded for the given capture.</summary>
    Task<IReadOnlyList<SkillRun>> GetByCaptureIdAsync(Guid captureId, CancellationToken cancellationToken = default);

    /// <summary>Returns all skill runs for a given skill name (e.g. for health/metrics).</summary>
    Task<IReadOnlyList<SkillRun>> GetBySkillNameAsync(string skillName, CancellationToken cancellationToken = default);
}

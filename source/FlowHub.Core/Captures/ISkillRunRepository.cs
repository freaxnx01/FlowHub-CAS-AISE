namespace FlowHub.Core.Captures;

public interface ISkillRunRepository
{
    Task<SkillRun> AddAsync(SkillRun skillRun, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SkillRun>> GetByCaptureIdAsync(Guid captureId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SkillRun>> GetBySkillNameAsync(string skillName, CancellationToken cancellationToken = default);
}

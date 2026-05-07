namespace FlowHub.Core.Health;

public interface ISkillRepository
{
    Task<IReadOnlyList<SkillHealth>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<SkillHealth?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task UpsertAsync(SkillHealth skill, CancellationToken cancellationToken = default);
    Task IncrementRoutedTodayAsync(string name, CancellationToken cancellationToken = default);
}

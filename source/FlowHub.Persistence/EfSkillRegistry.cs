using FlowHub.Core.Health;

namespace FlowHub.Persistence;

public sealed class EfSkillRegistry : ISkillRegistry
{
    private readonly ISkillRepository _repository;

    public EfSkillRegistry(ISkillRepository repository) => _repository = repository;

    public Task<IReadOnlyList<SkillHealth>> GetHealthAsync(CancellationToken cancellationToken = default) =>
        _repository.GetAllAsync(cancellationToken);
}

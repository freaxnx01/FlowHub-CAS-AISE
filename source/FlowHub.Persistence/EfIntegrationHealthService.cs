using FlowHub.Core.Health;

namespace FlowHub.Persistence;

public sealed class EfIntegrationHealthService : IIntegrationHealthService
{
    private readonly IIntegrationRepository _repository;

    public EfIntegrationHealthService(IIntegrationRepository repository) => _repository = repository;

    public Task<IReadOnlyList<IntegrationHealth>> GetHealthAsync(CancellationToken cancellationToken = default) =>
        _repository.GetAllAsync(cancellationToken);
}

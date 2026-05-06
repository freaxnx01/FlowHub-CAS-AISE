namespace FlowHub.Core.Health;

public interface IIntegrationRepository
{
    Task<IReadOnlyList<IntegrationHealth>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IntegrationHealth?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task UpsertAsync(IntegrationHealth integration, CancellationToken cancellationToken = default);
    Task AddHealthSampleAsync(string integrationName, HealthStatus status, TimeSpan? duration, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IntegrationHealthSample>> GetRecentSamplesAsync(string integrationName, int count, CancellationToken cancellationToken = default);
}

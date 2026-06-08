namespace FlowHub.Core.Health;

/// <summary>
/// Driven port for downstream integration health (<see cref="IntegrationHealth"/>)
/// and its time-series samples. EF Core implementation in <c>FlowHub.Persistence</c>.
/// </summary>
public interface IIntegrationRepository
{
    /// <summary>Returns current health for all known integrations.</summary>
    Task<IReadOnlyList<IntegrationHealth>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns health for a single integration, or <c>null</c> if unknown.</summary>
    Task<IntegrationHealth?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>Inserts or updates an integration's health record (keyed by name).</summary>
    Task UpsertAsync(IntegrationHealth integration, CancellationToken cancellationToken = default);

    /// <summary>Appends a health probe sample (status + optional latency) for an integration.</summary>
    Task AddHealthSampleAsync(string integrationName, HealthStatus status, TimeSpan? duration, CancellationToken cancellationToken = default);

    /// <summary>Returns the most recent <paramref name="count"/> health samples for an integration.</summary>
    Task<IReadOnlyList<IntegrationHealthSample>> GetRecentSamplesAsync(string integrationName, int count, CancellationToken cancellationToken = default);
}

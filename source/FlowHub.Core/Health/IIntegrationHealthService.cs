namespace FlowHub.Core.Health;

/// <summary>
/// Driving port for querying downstream Integration health.
/// </summary>
public interface IIntegrationHealthService
{
    Task<IReadOnlyList<IntegrationHealth>> GetHealthAsync(CancellationToken cancellationToken = default);
}

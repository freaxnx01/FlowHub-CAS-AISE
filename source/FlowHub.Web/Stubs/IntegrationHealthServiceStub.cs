using FlowHub.Core.Health;

namespace FlowHub.Web.Stubs;

/// <summary>
/// In-memory stub for <see cref="IIntegrationHealthService"/>.
/// </summary>
public sealed class IntegrationHealthServiceStub : IIntegrationHealthService
{
    public Task<IReadOnlyList<IntegrationHealth>> GetHealthAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        IReadOnlyList<IntegrationHealth> snapshot =
        [
            new("Wallabag",   HealthStatus.Healthy,  now.AddMinutes(-1),  TimeSpan.FromMilliseconds(180)),
            new("Vikunja",    HealthStatus.Healthy,  now.AddMinutes(-4),  TimeSpan.FromMilliseconds(150)),
            new("Paperless",  HealthStatus.Healthy,  now.AddHours(-2),    TimeSpan.FromMilliseconds(310)),
            new("Authentik",  HealthStatus.Healthy,  null,                null),
        ];
        return Task.FromResult(snapshot);
    }
}

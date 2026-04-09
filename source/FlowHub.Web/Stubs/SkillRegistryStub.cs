using FlowHub.Core.Health;

namespace FlowHub.Web.Stubs;

/// <summary>
/// In-memory stub for <see cref="ISkillRegistry"/>.
/// </summary>
public sealed class SkillRegistryStub : ISkillRegistry
{
    private static readonly IReadOnlyList<SkillHealth> Snapshot =
    [
        new("Books",     HealthStatus.Healthy,  42),
        new("Movies",    HealthStatus.Healthy,   8),
        new("Articles",  HealthStatus.Healthy,  15),
        new("Quotes",    HealthStatus.Degraded,  2),
        new("Knowledge", HealthStatus.Healthy,   3),
        new("Belege",    HealthStatus.Healthy,   7),
    ];

    public Task<IReadOnlyList<SkillHealth>> GetHealthAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(Snapshot);
}

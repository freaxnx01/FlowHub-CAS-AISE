using System.Diagnostics.CodeAnalysis;
using FlowHub.Core.Health;

namespace FlowHub.Web.Testing;

// E2E test scaffolding: only active when FLOWHUB_E2E_FAULTS_ENABLED=true.
// Excluded from coverage because exercising it requires the E2E browser pipeline.
[ExcludeFromCodeCoverage]
internal sealed class FaultInjectingSkillRegistry : ISkillRegistry
{
    private readonly ISkillRegistry _inner;
    private readonly IFaultInjector _faults;

    public FaultInjectingSkillRegistry(ISkillRegistry inner, IFaultInjector faults)
    {
        _inner = inner;
        _faults = faults;
    }

    public Task<IReadOnlyList<SkillHealth>> GetHealthAsync(CancellationToken cancellationToken = default)
    {
        if (_faults.SkillsShouldFail)
        {
            throw new InvalidOperationException("E2E fault: SkillRegistry forced to throw.");
        }
        return _inner.GetHealthAsync(cancellationToken);
    }
}

[ExcludeFromCodeCoverage]
internal sealed class FaultInjectingIntegrationHealthService : IIntegrationHealthService
{
    private readonly IIntegrationHealthService _inner;
    private readonly IFaultInjector _faults;

    public FaultInjectingIntegrationHealthService(IIntegrationHealthService inner, IFaultInjector faults)
    {
        _inner = inner;
        _faults = faults;
    }

    public Task<IReadOnlyList<IntegrationHealth>> GetHealthAsync(CancellationToken cancellationToken = default)
    {
        if (_faults.IntegrationsShouldFail)
        {
            throw new InvalidOperationException("E2E fault: IntegrationHealthService forced to throw.");
        }
        return _inner.GetHealthAsync(cancellationToken);
    }
}

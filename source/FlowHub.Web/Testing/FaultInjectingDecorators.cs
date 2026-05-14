using FlowHub.Core.Health;

namespace FlowHub.Web.Testing;

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

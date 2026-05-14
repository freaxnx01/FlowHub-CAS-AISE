namespace FlowHub.Web.Testing;

/// <summary>
/// Test-only switch that lets E2E specs force <see cref="FlowHub.Core.Health.ISkillRegistry"/>
/// or <see cref="FlowHub.Core.Health.IIntegrationHealthService"/> to throw on the next call.
/// Wired into DI only when <c>FLOWHUB_E2E_FAULTS_ENABLED=true</c> is set; in any other
/// environment the interface isn't registered and the decorators aren't installed.
///
/// Toggled via <c>POST /test/faults/{name}/arm</c> and <c>/disarm</c> — see
/// <see cref="E2EFaultExtensions"/>. bUnit owns the equivalent component-level negative
/// paths; this hook is purely so Playwright can assert the same surfaces end-to-end.
/// </summary>
public interface IFaultInjector
{
    bool SkillsShouldFail { get; set; }
    bool IntegrationsShouldFail { get; set; }
}

internal sealed class FaultInjector : IFaultInjector
{
    public bool SkillsShouldFail { get; set; }
    public bool IntegrationsShouldFail { get; set; }
}

using FlowHub.Core.Captures;

namespace FlowHub.Core.Skills;

/// <summary>
/// Driven port: writes a Capture to a downstream skill-specific service.
/// One method per integration; the implementation is responsible for HTTP,
/// auth, retries within a single attempt, and any skill-specific tagging.
/// </summary>
public interface ISkillIntegration
{
    string Name { get; }

    Task<SkillResult> HandleAsync(Capture capture, CancellationToken cancellationToken);
}

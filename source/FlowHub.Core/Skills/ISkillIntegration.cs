using FlowHub.Core.Captures;

namespace FlowHub.Core.Skills;

/// <summary>
/// Driven port: writes a Capture to a downstream skill-specific service.
/// Slice B ships <see cref="LoggingSkillIntegration"/> stubs; real adapters
/// (Wallabag, Wekan, Vikunja) land in Block 4/5.
/// </summary>
public interface ISkillIntegration
{
    string Name { get; }

    Task WriteAsync(Capture capture, IReadOnlyList<string> tags, CancellationToken cancellationToken);
}

using FlowHub.Core.Captures;
using FlowHub.Core.Skills;
using Microsoft.Extensions.Logging;

namespace FlowHub.Skills;

public sealed partial class LoggingSkillIntegration : ISkillIntegration
{
    private readonly ILogger<LoggingSkillIntegration> _logger;

    public LoggingSkillIntegration(string name, ILogger<LoggingSkillIntegration> logger)
    {
        Name = name;
        _logger = logger;
    }

    public string Name { get; }

    public Task<SkillResult> HandleAsync(Capture capture, CancellationToken cancellationToken)
    {
        LogStubWrite(Name, capture.Id);
        return Task.FromResult(new SkillResult(Success: true, ExternalRef: $"stub-{capture.Id:N}"));
    }

    [LoggerMessage(
        EventId = 2001,
        Level = LogLevel.Information,
        Message = "Stub integration '{Skill}' would write capture {CaptureId}")]
    private partial void LogStubWrite(string skill, Guid captureId);
}

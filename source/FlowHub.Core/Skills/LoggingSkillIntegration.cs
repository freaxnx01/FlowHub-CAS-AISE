using FlowHub.Core.Captures;
using Microsoft.Extensions.Logging;

namespace FlowHub.Core.Skills;

public sealed partial class LoggingSkillIntegration : ISkillIntegration
{
    private readonly ILogger<LoggingSkillIntegration> _logger;

    public LoggingSkillIntegration(string name, ILogger<LoggingSkillIntegration> logger)
    {
        Name = name;
        _logger = logger;
    }

    public string Name { get; }

    public Task WriteAsync(Capture capture, IReadOnlyList<string> tags, CancellationToken cancellationToken)
    {
        LogStubWrite(Name, capture.Id, tags);
        return Task.CompletedTask;
    }

    [LoggerMessage(
        EventId = 2001,
        Level = LogLevel.Information,
        Message = "Stub integration '{Skill}' would write capture {CaptureId} with tags {Tags}")]
    private partial void LogStubWrite(string skill, Guid captureId, IReadOnlyList<string> tags);
}

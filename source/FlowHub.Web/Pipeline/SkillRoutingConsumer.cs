using FlowHub.Core.Captures;
using FlowHub.Core.Events;
using FlowHub.Core.Skills;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace FlowHub.Web.Pipeline;

public sealed partial class SkillRoutingConsumer : IConsumer<CaptureClassified>
{
    private readonly IEnumerable<ISkillIntegration> _integrations;
    private readonly ICaptureService _captureService;
    private readonly ILogger<SkillRoutingConsumer> _logger;

    public SkillRoutingConsumer(
        IEnumerable<ISkillIntegration> integrations,
        ICaptureService captureService,
        ILogger<SkillRoutingConsumer> logger)
    {
        _integrations = integrations;
        _captureService = captureService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CaptureClassified> context)
    {
        var msg = context.Message;
        var ct = context.CancellationToken;

        var integration = _integrations.FirstOrDefault(i =>
            string.Equals(i.Name, msg.MatchedSkill, StringComparison.Ordinal));

        if (integration is null)
        {
            await _captureService.MarkUnhandledAsync(
                msg.CaptureId,
                $"no integration registered for skill '{msg.MatchedSkill}'",
                ct);
            LogUnhandled(msg.CaptureId, msg.MatchedSkill);
            return;
        }

        var capture = await _captureService.GetByIdAsync(msg.CaptureId, ct)
            ?? throw new InvalidOperationException($"Capture {msg.CaptureId} not found in store.");

        await integration.WriteAsync(capture, msg.Tags, ct);
        await _captureService.MarkRoutedAsync(msg.CaptureId, ct);
    }

    [LoggerMessage(EventId = 1002, Level = LogLevel.Information,
        Message = "Capture {CaptureId} marked Unhandled — no integration for skill {Skill}")]
    private partial void LogUnhandled(Guid captureId, string skill);
}

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

        await _captureService.MarkRoutedAsync(msg.CaptureId, ct);

        LogIntegrationCalled(integration.Name, msg.CaptureId);
        var result = await integration.HandleAsync(capture, ct);

        if (!result.Success)
        {
            // Throw to engage MassTransit's retry policy; if all attempts return Success=false
            // the LifecycleFaultObserver picks up the exhausted Fault<CaptureClassified> and
            // marks the capture Unhandled.
            throw new InvalidOperationException(
                result.FailureReason ?? $"skill '{integration.Name}' returned non-success without reason");
        }

        await _captureService.MarkCompletedAsync(msg.CaptureId, result.ExternalRef, ct);
        LogIntegrationSucceeded(integration.Name, msg.CaptureId, result.ExternalRef);
    }

    [LoggerMessage(EventId = 1002, Level = LogLevel.Information,
        Message = "Capture {CaptureId} marked Unhandled — no integration for skill {Skill}")]
    private partial void LogUnhandled(Guid captureId, string skill);

    [LoggerMessage(EventId = 2010, Level = LogLevel.Debug,
        Message = "Calling skill integration {Skill} for capture {CaptureId}")]
    private partial void LogIntegrationCalled(string skill, Guid captureId);

    [LoggerMessage(EventId = 2011, Level = LogLevel.Information,
        Message = "Skill integration {Skill} succeeded for capture {CaptureId} (externalRef={ExternalRef})")]
    private partial void LogIntegrationSucceeded(string skill, Guid captureId, string? externalRef);
}

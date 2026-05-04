using FlowHub.Core.Captures;
using FlowHub.Core.Classification;
using FlowHub.Core.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace FlowHub.Web.Pipeline;

public sealed partial class CaptureEnrichmentConsumer : IConsumer<CaptureCreated>
{
    private readonly IClassifier _classifier;
    private readonly ICaptureService _captureService;
    private readonly ILogger<CaptureEnrichmentConsumer> _logger;

    public CaptureEnrichmentConsumer(
        IClassifier classifier,
        ICaptureService captureService,
        ILogger<CaptureEnrichmentConsumer> logger)
    {
        _classifier = classifier;
        _captureService = captureService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CaptureCreated> context)
    {
        var msg = context.Message;
        var ct = context.CancellationToken;

        var result = await _classifier.ClassifyAsync(msg.Content, ct);

        if (string.IsNullOrEmpty(result.MatchedSkill))
        {
            await _captureService.MarkOrphanAsync(msg.CaptureId, "no skill matched during classification", ct);
            LogOrphan(msg.CaptureId);
            return;
        }

        await _captureService.MarkClassifiedAsync(msg.CaptureId, result.MatchedSkill, result.Title, ct);

        await context.Publish(new CaptureClassified(
            msg.CaptureId,
            result.Tags,
            result.MatchedSkill,
            DateTimeOffset.UtcNow));
    }

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "Capture {CaptureId} classified as Orphan (no matched skill)")]
    private partial void LogOrphan(Guid captureId);
}

using FlowHub.AI;
using FlowHub.Core.Captures;
using FlowHub.Core.Classification;
using FlowHub.Core.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace FlowHub.Web.Pipeline;

public sealed partial class CaptureEnrichmentConsumer : IConsumer<CaptureCreated>
{
    private readonly IClassifier _classifier;
    private readonly EnricherDispatcher _enricher;
    private readonly ICaptureService _captureService;
    private readonly ILogger<CaptureEnrichmentConsumer> _logger;

    public CaptureEnrichmentConsumer(
        IClassifier classifier,
        EnricherDispatcher enricher,
        ICaptureService captureService,
        ILogger<CaptureEnrichmentConsumer> logger)
    {
        _classifier = classifier;
        _enricher = enricher;
        _captureService = captureService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CaptureCreated> context)
    {
        var msg = context.Message;
        var ct = context.CancellationToken;

        if (msg.HasAttachment)
        {
            await _captureService.MarkClassifiedAsync(msg.CaptureId, "Paperless", cancellationToken: ct);
            await context.Publish(new CaptureClassified(
                msg.CaptureId,
                Tags: ["document"],
                MatchedSkill: "Paperless",
                ClassifiedAt: DateTimeOffset.UtcNow), ct);
            return;
        }

        var result = await _classifier.ClassifyAsync(msg.Content, ct);

        if (string.IsNullOrEmpty(result.MatchedSkill))
        {
            await _captureService.MarkOrphanAsync(msg.CaptureId, "no skill matched during classification", ct);
            LogOrphan(msg.CaptureId);
            return;
        }

        // Skip the DB round-trip + enricher dispatch for non-Vikunja captures —
        // dispatcher would early-return (null, null) anyway.
        string? project = null;
        EnrichmentResult? enrichment = null;
        if (string.Equals(result.MatchedSkill, "Vikunja", StringComparison.Ordinal))
        {
            var capture = await _captureService.GetByIdAsync(msg.CaptureId, ct)
                ?? throw new InvalidOperationException($"Capture {msg.CaptureId} not found in store.");

            (project, enrichment) = await _enricher.DispatchAsync(capture, result, ct);
        }

        await _captureService.MarkClassifiedAsync(
            msg.CaptureId,
            result.MatchedSkill,
            result.Title,
            project,
            enrichment?.Description,
            result.Trace,
            ct);

        await context.Publish(new CaptureClassified(
            msg.CaptureId,
            result.Tags,
            result.MatchedSkill,
            DateTimeOffset.UtcNow,
            project,
            enrichment?.Description));
    }

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "Capture {CaptureId} classified as Orphan (no matched skill)")]
    private partial void LogOrphan(Guid captureId);
}

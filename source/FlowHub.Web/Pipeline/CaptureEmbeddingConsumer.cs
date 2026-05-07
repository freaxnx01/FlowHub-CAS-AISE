using FlowHub.Core.Captures;
using FlowHub.Core.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace FlowHub.Web.Pipeline;

// Embedding is generated off the request path so POST /api/v1/captures stays inside its
// NF-09 p95 < 200 ms budget. A failed generation is non-fatal — the capture stays without
// embedding and can be backfilled via POST /api/v1/admin/embeddings/rebuild.
public sealed partial class CaptureEmbeddingConsumer : IConsumer<CaptureCreated>
{
    private readonly IEmbeddingService _embeddingService;
    private readonly ICaptureRepository _captureRepository;
    private readonly ILogger<CaptureEmbeddingConsumer> _logger;

    public CaptureEmbeddingConsumer(
        IEmbeddingService embeddingService,
        ICaptureRepository captureRepository,
        ILogger<CaptureEmbeddingConsumer> logger)
    {
        _embeddingService = embeddingService;
        _captureRepository = captureRepository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CaptureCreated> context)
    {
        var msg = context.Message;
        var ct = context.CancellationToken;

        var embedding = await _embeddingService.GenerateAsync(msg.Content, ct);
        if (embedding is null)
        {
            LogSkipped(msg.CaptureId);
            return;
        }

        await _captureRepository.StoreEmbeddingAsync(msg.CaptureId, embedding, ct);
    }

    [LoggerMessage(EventId = 6010, Level = LogLevel.Debug,
        Message = "Skipped embedding for Capture {CaptureId} (provider unconfigured or transient failure)")]
    private partial void LogSkipped(Guid captureId);
}

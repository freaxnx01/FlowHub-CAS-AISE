using FlowHub.Core.Captures;
using FlowHub.Core.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace FlowHub.Web.Pipeline;

/// <summary>
/// Best-effort pipeline branch (ADR 0006): consumes <c>CaptureCreated</c> and
/// generates the capture's vector embedding off the request path, so
/// <c>POST /api/v1/captures</c> stays inside its NF-09 p95 &lt; 200 ms budget. A
/// failed generation is non-fatal — the capture stays without an embedding and can
/// be backfilled via <c>POST /api/v1/admin/embeddings/rebuild</c>.
/// </summary>
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

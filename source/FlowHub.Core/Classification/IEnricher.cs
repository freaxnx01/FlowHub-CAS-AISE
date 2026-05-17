using FlowHub.Core.Captures;

namespace FlowHub.Core.Classification;

/// <summary>
/// Driven port for per-bucket capture enrichment. Implementations are registered by
/// bucket name and invoked by <c>EnricherDispatcher</c> when a Capture's
/// <see cref="ClassificationResult.VikunjaProject"/> matches.
/// </summary>
public interface IEnricher
{
    string BucketName { get; }

    Task<EnrichmentResult?> EnrichAsync(
        Capture capture,
        ClassificationResult classification,
        CancellationToken cancellationToken);
}

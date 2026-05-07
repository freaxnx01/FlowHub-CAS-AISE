namespace FlowHub.Core.Captures;

/// <summary>
/// Port for generating vector embeddings from text.
/// Implemented by <c>FlowHub.AI.AiEmbeddingService</c>; default is a no-op.
/// </summary>
public interface IEmbeddingService
{
    /// <returns>Embedding vector, or <c>null</c> if the service is not configured or generation fails.</returns>
    Task<float[]?> GenerateAsync(string text, CancellationToken cancellationToken = default);
}

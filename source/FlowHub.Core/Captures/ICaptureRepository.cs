namespace FlowHub.Core.Captures;

/// <summary>
/// Driven port for persisting and querying <see cref="Capture"/> aggregates.
/// The domain depends on this abstraction; the EF Core implementation
/// (<c>EfCaptureRepository</c>) lives in <c>FlowHub.Persistence</c>.
/// </summary>
public interface ICaptureRepository
{
    /// <summary>Returns the capture with the given id, or <c>null</c> if none exists.</summary>
    Task<Capture?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns all captures (unbounded; intended for small datasets / admin use).</summary>
    Task<IReadOnlyList<Capture>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns the <paramref name="count"/> most recently created captures.</summary>
    Task<IReadOnlyList<Capture>> GetRecentAsync(int count, CancellationToken cancellationToken = default);

    /// <summary>Returns aggregate counts of captures in failure lifecycle stages (Orphan/Unhandled).</summary>
    Task<FailureCounts> GetFailureCountsAsync(CancellationToken cancellationToken = default);

    /// <summary>Persists a new capture and returns the stored instance.</summary>
    Task<Capture> AddAsync(Capture capture, CancellationToken cancellationToken = default);

    /// <summary>Persists changes to an existing capture (lifecycle transitions, enrichment results).</summary>
    Task UpdateAsync(Capture capture, CancellationToken cancellationToken = default);

    /// <summary>Returns a cursor-paginated, filtered page of captures (the hot dashboard/list read path).</summary>
    Task<CapturePage> ListAsync(CaptureFilter filter, CancellationToken cancellationToken = default);

    /// <summary>Stores the embedding vector for a capture (pgvector-backed semantic search).</summary>
    Task StoreEmbeddingAsync(Guid captureId, float[] embedding, CancellationToken cancellationToken = default);

    /// <summary>Returns the <paramref name="limit"/> captures nearest to <paramref name="queryEmbedding"/> by vector distance.</summary>
    Task<IReadOnlyList<Capture>> SearchByEmbeddingAsync(float[] queryEmbedding, int limit, CancellationToken cancellationToken = default);

    /// <summary>Returns ids of captures that have no embedding yet (used to backfill the vector index).</summary>
    Task<IReadOnlyList<Guid>> GetIdsWithoutEmbeddingAsync(CancellationToken cancellationToken = default);
}

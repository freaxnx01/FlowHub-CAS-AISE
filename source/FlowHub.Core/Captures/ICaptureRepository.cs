namespace FlowHub.Core.Captures;

public interface ICaptureRepository
{
    Task<Capture?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Capture>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Capture>> GetRecentAsync(int count, CancellationToken cancellationToken = default);
    Task<FailureCounts> GetFailureCountsAsync(CancellationToken cancellationToken = default);
    Task<Capture> AddAsync(Capture capture, CancellationToken cancellationToken = default);
    Task UpdateAsync(Capture capture, CancellationToken cancellationToken = default);
    Task<CapturePage> ListAsync(CaptureFilter filter, CancellationToken cancellationToken = default);
}

namespace FlowHub.Core.Captures;

/// <summary>
/// Driving port for the Capture aggregate.
/// Implementations live in module/infrastructure projects; the Web UI
/// consumes this interface directly via DI without going through HTTP.
/// </summary>
public interface ICaptureService
{
    Task<Capture?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Capture>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Capture>> GetRecentAsync(int count, CancellationToken cancellationToken = default);

    Task<FailureCounts> GetFailureCountsAsync(CancellationToken cancellationToken = default);

    Task<Capture> SubmitAsync(string content, ChannelKind source, CancellationToken cancellationToken = default);
}

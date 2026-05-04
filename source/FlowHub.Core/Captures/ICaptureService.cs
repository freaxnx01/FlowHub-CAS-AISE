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

    Task MarkClassifiedAsync(Guid id, string matchedSkill, string? title = null, CancellationToken cancellationToken = default);

    Task MarkRoutedAsync(Guid id, CancellationToken cancellationToken = default);

    Task MarkCompletedAsync(Guid id, string? externalRef, CancellationToken cancellationToken = default);

    Task MarkOrphanAsync(Guid id, string reason, CancellationToken cancellationToken = default);

    Task MarkUnhandledAsync(Guid id, string reason, CancellationToken cancellationToken = default);

    Task<CapturePage> ListAsync(CaptureFilter filter, CancellationToken cancellationToken = default);

    Task ResetForRetryAsync(Guid id, CancellationToken cancellationToken = default);
}

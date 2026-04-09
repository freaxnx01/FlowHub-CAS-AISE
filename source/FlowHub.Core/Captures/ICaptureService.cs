namespace FlowHub.Core.Captures;

/// <summary>
/// Driving port for the Capture aggregate.
/// Implementations live in module/infrastructure projects; the Web UI
/// consumes this interface directly via DI without going through HTTP.
/// </summary>
public interface ICaptureService
{
    Task<IReadOnlyList<Capture>> GetRecentAsync(int count, CancellationToken cancellationToken = default);

    Task<FailureCounts> GetFailureCountsAsync(CancellationToken cancellationToken = default);

    Task<Capture> SubmitAsync(string content, ChannelKind source, CancellationToken cancellationToken = default);
}

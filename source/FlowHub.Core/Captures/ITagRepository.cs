namespace FlowHub.Core.Captures;

/// <summary>
/// Driven port for the tags attached to a <see cref="Capture"/>. EF Core
/// implementation in <c>FlowHub.Persistence</c>.
/// </summary>
public interface ITagRepository
{
    /// <summary>Returns the tag values attached to the given capture.</summary>
    Task<IReadOnlyList<string>> GetByCaptureIdAsync(Guid captureId, CancellationToken cancellationToken = default);

    /// <summary>Attaches a tag value to the capture (no-op if already present).</summary>
    Task AddAsync(Guid captureId, string value, CancellationToken cancellationToken = default);

    /// <summary>Removes a tag value from the capture.</summary>
    Task RemoveAsync(Guid captureId, string value, CancellationToken cancellationToken = default);
}

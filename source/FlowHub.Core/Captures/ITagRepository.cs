namespace FlowHub.Core.Captures;

public interface ITagRepository
{
    Task<IReadOnlyList<string>> GetByCaptureIdAsync(Guid captureId, CancellationToken cancellationToken = default);
    Task AddAsync(Guid captureId, string value, CancellationToken cancellationToken = default);
    Task RemoveAsync(Guid captureId, string value, CancellationToken cancellationToken = default);
}

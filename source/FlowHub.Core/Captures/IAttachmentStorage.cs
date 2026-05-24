namespace FlowHub.Core.Captures;

public interface IAttachmentStorage
{
    /// <returns>Relative storage path (portable across machines).</returns>
    Task<string> SaveAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>Best-effort delete used to roll back a failed Capture save.</summary>
    Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default);
}

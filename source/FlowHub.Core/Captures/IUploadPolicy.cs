namespace FlowHub.Core.Captures;

/// <summary>
/// Live view over <see cref="UploadOptions"/>. Components depend on this
/// instead of taking IOptions&lt;T&gt; directly.
/// </summary>
public interface IUploadPolicy
{
    long MaxBytes { get; }
    IReadOnlyList<string> AllowedContentTypes { get; }
    string AcceptAttribute { get; }
}

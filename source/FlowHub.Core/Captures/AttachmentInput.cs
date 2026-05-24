namespace FlowHub.Core.Captures;

/// <summary>
/// Transient transfer object carrying upload bytes + metadata into the
/// Capture submission pipeline. Never persisted.
/// </summary>
public sealed class AttachmentInput
{
    public required Stream Content { get; init; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public required long SizeBytes { get; init; }
}

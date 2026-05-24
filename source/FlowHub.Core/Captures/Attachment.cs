namespace FlowHub.Core.Captures;

/// <summary>
/// Binary content attached to a <see cref="Capture"/>. Value object, persisted
/// as an EF Core owned entity. Bytes live on the filesystem; this record stores
/// only metadata + a relative storage path.
/// </summary>
public sealed record Attachment(
    string FileName,
    string ContentType,
    long SizeBytes,
    string RelativePath,
    DateTimeOffset UploadedAt);

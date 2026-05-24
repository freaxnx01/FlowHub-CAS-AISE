namespace FlowHub.Persistence.Entities;

internal sealed class AttachmentEntity
{
    public string FileName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long SizeBytes { get; set; }
    public string RelativePath { get; set; } = "";
    public DateTimeOffset UploadedAt { get; set; }
}

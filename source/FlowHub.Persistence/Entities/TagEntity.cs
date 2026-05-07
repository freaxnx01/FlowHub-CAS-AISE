namespace FlowHub.Persistence.Entities;

internal sealed class TagEntity
{
    public Guid CaptureId { get; set; }
    public string Value { get; set; } = "";
    public CaptureEntity Capture { get; set; } = null!;
}

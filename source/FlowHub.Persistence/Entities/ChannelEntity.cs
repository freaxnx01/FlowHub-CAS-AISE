namespace FlowHub.Persistence.Entities;

internal sealed class ChannelEntity
{
    public string Name { get; set; } = "";
    public string Kind { get; set; } = "";
    public bool IsEnabled { get; set; }
    public string Status { get; set; } = "";
    public DateTimeOffset? LastActiveAt { get; set; }
}

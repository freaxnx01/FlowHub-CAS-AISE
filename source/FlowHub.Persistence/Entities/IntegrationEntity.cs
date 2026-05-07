namespace FlowHub.Persistence.Entities;

internal sealed class IntegrationEntity
{
    public string Name { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTimeOffset? LastWriteAt { get; set; }
    public long? LastWriteDurationMs { get; set; }
    public ICollection<IntegrationHealthSampleEntity> Samples { get; set; } = [];
}

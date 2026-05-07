namespace FlowHub.Persistence.Entities;

internal sealed class IntegrationHealthSampleEntity
{
    public Guid Id { get; set; }
    public string IntegrationName { get; set; } = "";
    public DateTimeOffset SampledAt { get; set; }
    public string Status { get; set; } = "";
    public long? DurationMs { get; set; }
    public IntegrationEntity Integration { get; set; } = null!;
}

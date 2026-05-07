namespace FlowHub.Core.Health;

public sealed record IntegrationHealthSample(
    Guid Id,
    string IntegrationName,
    DateTimeOffset SampledAt,
    HealthStatus Status,
    TimeSpan? Duration);

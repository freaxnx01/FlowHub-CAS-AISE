using System.Text.Json.Serialization;

namespace FlowHub.AI.Classification.Models;

public record ClassificationResult
{
    [JsonPropertyName("skill")]
    public string Skill { get; init; } = string.Empty;

    [JsonPropertyName("confidence")]
    public double Confidence { get; init; }

    [JsonPropertyName("reasoning")]
    public string Reasoning { get; init; } = string.Empty;

    // Not from JSON — set by ClassificationService
    [JsonIgnore]
    public string ModelName { get; init; } = string.Empty;

    [JsonIgnore]
    public TimeSpan Latency { get; init; }

    [JsonIgnore]
    public string? Error { get; init; }

    public bool IsError => Error is not null;
}

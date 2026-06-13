namespace FlowHub.Persistence.Entities;

/// <summary>EF owned-entity shape of <see cref="FlowHub.Core.Classification.ClassifierTrace"/>.</summary>
internal sealed class ClassifierTraceOwned
{
    public string Kind { get; set; } = "";
    public int LatencyMs { get; set; }
    public string? Provider { get; set; }
    public string? Model { get; set; }
    public int? PromptTokens { get; set; }
    public int? CompletionTokens { get; set; }
}

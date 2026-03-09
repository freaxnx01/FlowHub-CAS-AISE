namespace FlowHub.AI.Classification.Models;

public record ModelConfig
{
    public string Id { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
}

namespace FlowHub.Core.Classification;

/// <summary>Which classifier produced a result.</summary>
public enum ClassifierKind
{
    Ai,
    Keyword,
}

/// <summary>
/// Telemetry from a single classification call, surfaced by the debug/trace mode.
/// <paramref name="Provider"/>, <paramref name="Model"/>, <paramref name="PromptTokens"/>
/// and <paramref name="CompletionTokens"/> are populated only for <see cref="ClassifierKind.Ai"/>.
/// </summary>
public sealed record ClassifierTrace(
    ClassifierKind Kind,
    int LatencyMs,
    string? Provider = null,
    string? Model = null,
    int? PromptTokens = null,
    int? CompletionTokens = null);

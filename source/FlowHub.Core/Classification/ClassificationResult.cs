namespace FlowHub.Core.Classification;

/// <summary>
/// Output of <see cref="IClassifier.ClassifyAsync"/>.
/// Slice B (KeywordClassifier) returns Title=null; Slice C (AiClassifier) populates it
/// in the same round-trip as Tags + MatchedSkill (per ADR 0004 D4).
/// </summary>
public sealed record ClassificationResult(
    IReadOnlyList<string> Tags,
    string MatchedSkill,
    string? Title = null);

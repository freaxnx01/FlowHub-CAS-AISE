namespace FlowHub.Core.Classification;

/// <summary>
/// Output of <see cref="IClassifier.ClassifyAsync"/>.
/// Slice B (KeywordClassifier) returns Title=null; Slice C (AiClassifier) populates Title
/// in the same round-trip as Tags + MatchedSkill (per ADR 0004 D4). Block 5 adds
/// VikunjaProject (the bucket name the classifier routed to) and Entities (structured
/// fields extracted by the model for downstream enrichers).
/// </summary>
public sealed record ClassificationResult(
    IReadOnlyList<string> Tags,
    string MatchedSkill,
    string? Title = null,
    string? VikunjaProject = null,
    IReadOnlyDictionary<string, string>? Entities = null,
    ClassifierTrace? Trace = null);

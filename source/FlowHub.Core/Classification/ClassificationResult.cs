namespace FlowHub.Core.Classification;

public sealed record ClassificationResult(
    IReadOnlyList<string> Tags,
    string MatchedSkill);

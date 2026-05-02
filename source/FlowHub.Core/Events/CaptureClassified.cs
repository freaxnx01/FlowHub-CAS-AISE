namespace FlowHub.Core.Events;

public sealed record CaptureClassified(
    Guid CaptureId,
    IReadOnlyList<string> Tags,
    string MatchedSkill,
    DateTimeOffset ClassifiedAt);

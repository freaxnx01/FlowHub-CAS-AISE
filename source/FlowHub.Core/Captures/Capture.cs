namespace FlowHub.Core.Captures;

/// <summary>
/// The central FlowHub noun. A single piece of incoming content from any
/// channel — URL, text, image reference, voice memo, etc.
/// See Glossary entry "Capture" in the CAS Obsidian vault.
/// </summary>
public sealed record Capture(
    Guid Id,
    ChannelKind Source,
    string Content,
    DateTimeOffset CreatedAt,
    LifecycleStage Stage,
    string? MatchedSkill,
    string? FailureReason = null,
    string? Title = null,
    string? ExternalRef = null);

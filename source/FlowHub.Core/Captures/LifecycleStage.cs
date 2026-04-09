namespace FlowHub.Core.Captures;

/// <summary>
/// Lifecycle stage of a <see cref="Capture"/>.
/// See Glossary entry "Capture lifecycle" in the CAS Obsidian vault.
/// </summary>
public enum LifecycleStage
{
    /// <summary>Just arrived, no classification yet.</summary>
    Raw,

    /// <summary>AI has assigned a category / target Skill.</summary>
    Classified,

    /// <summary>Successfully handed off to a Skill.</summary>
    Routed,

    /// <summary>A Skill exists, but processing failed.</summary>
    Orphan,

    /// <summary>No matching Skill — triggers a Skill suggestion.</summary>
    Unhandled,
}

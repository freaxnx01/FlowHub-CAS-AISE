namespace FlowHub.Skills;

/// <summary>
/// Result of evaluating one <c>Skills:&lt;X&gt;</c> configuration section. Aggregated by
/// <see cref="SkillsBootLogger"/> to produce one boot log line per configured skill.
/// </summary>
public sealed record SkillsRegistrationOutcome(string Skill, bool Registered, string Reason);

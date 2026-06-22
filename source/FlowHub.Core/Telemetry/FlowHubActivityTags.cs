using System.Diagnostics;

namespace FlowHub.Core.Telemetry;

// ADR 0009 §5 — central, type-safe helpers for setting `flowhub.*` activity tags.
// Direct `Activity.SetTag("flowhub.…", …)` calls outside this class are discouraged;
// the `TagAllowListProcessor` strips any unknown `flowhub.*` tag at export time as the
// second line of defense. Keep this list aligned with `TagAllowListProcessor.FlowHubAllowList`.
public static class FlowHubActivityTags
{
    public const string Source = "FlowHub";

    public static Activity? SetCaptureId(this Activity? activity, Guid captureId) =>
        activity?.SetTag("flowhub.capture_id", captureId.ToString("D"));

    public static Activity? SetStage(this Activity? activity, string stage) =>
        activity?.SetTag("flowhub.stage", stage);

    public static Activity? SetClassificationSource(this Activity? activity, string source) =>
        activity?.SetTag("flowhub.classification_source", source);

    public static Activity? SetMatchedSkill(this Activity? activity, string? skill) =>
        activity?.SetTag("flowhub.matched_skill", skill ?? string.Empty);

    public static Activity? SetSkillName(this Activity? activity, string name) =>
        activity?.SetTag("flowhub.skill.name", name);

    public static Activity? SetSkillOutcome(this Activity? activity, string outcome) =>
        activity?.SetTag("flowhub.skill.outcome", outcome);

    public static Activity? SetBodyLength(this Activity? activity, int length) =>
        activity?.SetTag("flowhub.body_length", length);

    public static Activity? SetTagCount(this Activity? activity, int count) =>
        activity?.SetTag("flowhub.tag_count", count);

    public static Activity? SetFallback(this Activity? activity, bool fallback) =>
        activity?.SetTag("flowhub.fallback", fallback);

    public static Activity? SetReason(this Activity? activity, string reason) =>
        activity?.SetTag("flowhub.reason", reason);
}

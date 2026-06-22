using System.Diagnostics;
using OpenTelemetry;

namespace FlowHub.Web.Observability;

// Defense-in-depth processor for OTel tracing — ADR 0009.
// §1: only the allow-listed `flowhub.*` keys survive; unknown flowhub-tags are stripped.
// §2: forbidden tags from third-party instrumentation (http bodies, db.statement, gen_ai
//     prompts/completions, *.email/*.username/*.user.id) are stripped before export.
// §4: string values longer than 256 chars are redacted to "<redacted:length=N>".
internal sealed class TagAllowListProcessor : BaseProcessor<Activity>
{
    private const int MaxStringTagLength = 256;

    // ADR 0009 §1 — closed set of permitted `flowhub.*` tag keys.
    private static readonly HashSet<string> FlowHubAllowList = new(StringComparer.Ordinal)
    {
        "flowhub.capture_id",
        "flowhub.stage",
        "flowhub.classification_source",
        "flowhub.matched_skill",
        "flowhub.skill.name",
        "flowhub.skill.outcome",
        "flowhub.body_length",
        "flowhub.tag_count",
        "flowhub.fallback",
        "flowhub.reason",
    };

    // ADR 0009 §2 — block-list (exact and prefix matches).
    private static readonly string[] ForbiddenPrefixes =
    {
        "http.request.body.",
        "http.response.body.",
        "messaging.message.payload",
    };

    private static readonly HashSet<string> ForbiddenExact = new(StringComparer.Ordinal)
    {
        "db.statement",
        "db.query.text",
        "gen_ai.prompt",
        "gen_ai.completion",
    };

    // Heuristic — keys ending in any of these indicate user identity.
    private static readonly string[] ForbiddenSuffixes =
    {
        ".email",
        ".username",
        ".user.id",
    };

    public override void OnEnd(Activity activity)
    {
        // Snapshot keys first — modifying tags during enumeration is not supported.
        List<string>? toRemove = null;
        List<KeyValuePair<string, object?>>? toTruncate = null;

        foreach (var tag in activity.TagObjects)
        {
            if (ShouldStrip(tag.Key))
            {
                (toRemove ??= new List<string>()).Add(tag.Key);
                continue;
            }

            if (tag.Value is string s && s.Length > MaxStringTagLength)
            {
                (toTruncate ??= new List<KeyValuePair<string, object?>>())
                    .Add(new KeyValuePair<string, object?>(tag.Key, $"<redacted:length={s.Length}>"));
            }
        }

        if (toRemove is not null)
        {
            foreach (var key in toRemove)
            {
                activity.SetTag(key, null); // setting null removes the tag
            }
        }

        if (toTruncate is not null)
        {
            foreach (var kv in toTruncate)
            {
                activity.SetTag(kv.Key, kv.Value);
            }
        }
    }

    private static bool ShouldStrip(string key)
    {
        if (key.StartsWith("flowhub.", StringComparison.Ordinal))
        {
            return !FlowHubAllowList.Contains(key);
        }

        if (ForbiddenExact.Contains(key))
        {
            return true;
        }

        foreach (var p in ForbiddenPrefixes)
        {
            if (key.StartsWith(p, StringComparison.Ordinal))
            {
                return true;
            }
        }

        foreach (var sfx in ForbiddenSuffixes)
        {
            if (key.EndsWith(sfx, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}

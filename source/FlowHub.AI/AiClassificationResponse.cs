using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace FlowHub.AI;

internal sealed record AiClassificationResponse(
    [property: Description("1–5 short lowercase tags describing the snippet")]
    [property: JsonPropertyName("tags")]
    string[] Tags,

    [property: Description("Wallabag, Vikunja, or empty string for none")]
    [property: AllowedValues("Wallabag", "Vikunja", "")]
    [property: JsonPropertyName("matched_skill")]
    string MatchedSkill,

    [property: Description("3–8 word title or null if content is too short")]
    [property: JsonPropertyName("title")]
    string? Title,

    [property: Description("Vikunja project bucket name when matched_skill=Vikunja; null otherwise")]
    [property: JsonPropertyName("project")]
    string? Project,

    [property: Description("Optional structured entities the bucket may consume (e.g. quote, author)")]
    [property: JsonPropertyName("entities")]
    Dictionary<string, string>? Entities);

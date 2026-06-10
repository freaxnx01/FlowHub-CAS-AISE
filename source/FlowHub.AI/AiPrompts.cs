using System.Globalization;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

[assembly: InternalsVisibleTo("FlowHub.Web.ComponentTests")]

namespace FlowHub.AI;

internal static class AiPrompts
{
    internal static string BuildSystemPrompt(IReadOnlyCollection<string> vikunjaBuckets)
    {
        var bucketLine = vikunjaBuckets.Count == 0
            ? "Inbox"
            : string.Join(", ", vikunjaBuckets);

        return string.Create(CultureInfo.InvariantCulture, $$"""
            You classify user-captured snippets for a personal knowledge tool called FlowHub.

            For each capture, return:
            - tags: 1–5 short lowercase tags describing the snippet
            - matched_skill: which downstream skill should handle it. Choose exactly ONE:
                "Wallabag"  – the snippet is a URL or article worth saving for later reading
                "Vikunja"   – the snippet is a task, todo, OR a structured piece of content
                              that belongs in a Vikunja project (quote, movie, book, …)
                ""          – none of the above; it will be marked as Orphan
            - project: when matched_skill="Vikunja", pick the best matching project from
              this list. If unsure, pick "Inbox".
                Available: {{bucketLine}}
              Leave empty otherwise.
            - title: a 3–8 word title summarising the snippet (omit only if the snippet
                     is itself shorter than 8 words)
            - entities: optional structured fields the project may use, e.g.
                Zitate → {"quote": "...", "author": "..."}
                Movies → {"title": "...", "year": "..."}
              Omit if nothing applies.

            Reply ONLY via the structured response schema. Never include explanations.
            """);
    }

    internal static IList<ChatMessage> BuildMessages(string content, IReadOnlyCollection<string> vikunjaBuckets) =>
    [
        new ChatMessage(ChatRole.System, BuildSystemPrompt(vikunjaBuckets)),
        new ChatMessage(ChatRole.User, content),
    ];
}

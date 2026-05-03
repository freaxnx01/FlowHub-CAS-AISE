using Microsoft.Extensions.AI;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("FlowHub.Web.ComponentTests")]

namespace FlowHub.AI;

internal static class AiPrompts
{
    internal const string SystemPrompt = """
        You classify user-captured snippets for a personal knowledge tool called FlowHub.

        For each capture, return:
        - tags: 1–5 short lowercase tags describing the snippet
        - matched_skill: which downstream skill should handle it. Choose exactly ONE:
            "Wallabag"  – the snippet is a URL or article worth saving for later reading
            "Vikunja"   – the snippet is a task, todo, or actionable item
            ""          – none of the above; it will be marked as Orphan
        - title: a 3–8 word title summarising the snippet (omit only if the snippet
                 is itself shorter than 8 words)

        Reply ONLY via the structured response schema. Never include explanations.
        """;

    internal static IList<ChatMessage> BuildMessages(string content) =>
    [
        new ChatMessage(ChatRole.System, SystemPrompt),
        new ChatMessage(ChatRole.User, content),
    ];
}

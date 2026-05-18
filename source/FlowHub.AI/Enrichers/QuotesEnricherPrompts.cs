using Microsoft.Extensions.AI;

namespace FlowHub.AI.Enrichers;

internal static class QuotesEnricherPrompts
{
    internal const string SystemPrompt =
        "You write a 2–3 sentence factual bio of a public figure for a personal knowledge tool. " +
        "If you don't know the person, reply with an empty string. Never invent facts.";

    internal static IList<ChatMessage> BuildMessages(string author) =>
    [
        new ChatMessage(ChatRole.System, SystemPrompt),
        new ChatMessage(ChatRole.User, author),
    ];
}

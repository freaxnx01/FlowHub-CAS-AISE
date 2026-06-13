using Microsoft.Extensions.AI;

namespace FlowHub.AI.Enrichers;

internal static class ZitateEnricherPrompts
{
    internal const string SystemPrompt =
        "You enrich a quotation for a personal knowledge tool. You are given an author " +
        "and their quote. Write 2–4 factual sentences covering: who the author is " +
        "(full name, life dates if known, nationality, and their role or field), and — " +
        "ONLY if you genuinely know it — roughly when or in what context the quote was " +
        "said or written (a year, decade, or occasion). If you do not recognise the " +
        "author, reply with an empty string. Never invent facts, dates, or attributions.";

    internal static IList<ChatMessage> BuildMessages(string author, string quote) =>
    [
        new ChatMessage(ChatRole.System, SystemPrompt),
        new ChatMessage(ChatRole.User, $"Author: {author}\nQuote: \"{quote.Trim('"', ' ', '\n')}\""),
    ];
}

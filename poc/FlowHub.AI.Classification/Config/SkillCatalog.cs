using FlowHub.AI.Classification.Models;

namespace FlowHub.AI.Classification.Config;

public static class SkillCatalog
{
    public static IReadOnlyList<SkillDefinition> Skills { get; } =
    [
        new("ArticleSkill", "News articles, blog posts, tech articles, read-later content"),
        new("HomelabSkill", "Self-hosted software, homelab services, server tools, Docker images"),
        new("BookSkill", "Books, e-books, audiobooks, reading recommendations"),
        new("MovieSkill", "Movies, TV series, streaming content, watchlists"),
        new("DocumentSkill", "PDFs, scanned documents, receipts, photos of paperwork"),
        new("KnowledgeSkill", "Facts, explanations, how-things-work, learning topics"),
        new("QuoteSkill", "Quotes, sayings, citations, memorable phrases"),
        new("GenericSkill", "Anything that doesn't fit the above categories"),
    ];

    public static string BuildSkillListPrompt()
    {
        return string.Join('\n', Skills.Select((s, i) =>
            $"{i + 1}. {s.Name} — {s.Description}"));
    }
}

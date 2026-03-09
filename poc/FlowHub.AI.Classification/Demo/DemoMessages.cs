namespace FlowHub.AI.Classification.Demo;

public record DemoMessage(string Text, string ExpectedSkill);

public static class DemoMessages
{
    public static IReadOnlyList<DemoMessage> All { get; } =
    [
        new("https://www.imdb.com/de/title/tt2543312/", "MovieSkill"),
        new("Muss ich unbedingt schauen!", "MovieSkill"),
        new("https://heise.de/news/some-article-12345", "ArticleSkill"),
        new("Interesting blog post about Rust memory safety", "ArticleSkill"),
        new("https://www.thalia.ch/shop/some-book", "BookSkill"),
        new("Dieses Buch muss ich unbedingt lesen", "BookSkill"),
        new("Check out Portainer for managing Docker containers", "HomelabSkill"),
        new("https://github.com/louislam/uptime-kuma", "HomelabSkill"),
        new("Photo of a receipt from the dentist", "DocumentSkill"),
        new("PDF: Steuererklaerung_2025.pdf", "DocumentSkill"),
        new("Wie funktioniert eigentlich DNS?", "KnowledgeSkill"),
        new("I want to understand how transformers work in ML", "KnowledgeSkill"),
        new("\"Be the change you wish to see in the world\"", "QuoteSkill"),
        new("Zitat: \"Der Weg ist das Ziel\"", "QuoteSkill"),
        new("Remind me to buy milk tomorrow", "GenericSkill"),
        new("Call mom on Sunday", "GenericSkill"),
    ];
}

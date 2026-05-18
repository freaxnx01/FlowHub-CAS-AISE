using FlowHub.AI.Enrichers;
using FlowHub.Core.Captures;
using FlowHub.Core.Classification;
using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace FlowHub.Web.ComponentTests.Classification;

public class QuotesEnricherTests
{
    private static Capture Sample() => new(
        Guid.NewGuid(), ChannelKind.Web,
        "\"Unix and C are the ultimate computer viruses.\", Richard Gabriel",
        DateTimeOffset.UtcNow, LifecycleStage.Raw, "Vikunja");

    private static ClassificationResult Classification(string? author) =>
        new(["quote"], "Vikunja", "Gabriel on Unix and C", "Quotes",
            author is null ? null : new Dictionary<string, string>
            {
                ["quote"] = "Unix and C are the ultimate computer viruses.",
                ["author"] = author,
            });

    [Fact]
    public async Task EnrichAsync_ComposesQuoteAndBioMarkdown()
    {
        var chat = Substitute.For<IChatClient>();
        chat.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions?>(), Arg.Any<CancellationToken>())
            .Returns(new ChatResponse(new ChatMessage(ChatRole.Assistant,
                "American computer scientist known for work on Lisp.")));

        var enricher = new QuotesEnricher(chat, NullLogger<QuotesEnricher>.Instance);

        var result = await enricher.EnrichAsync(Sample(), Classification("Richard Gabriel"), default);

        result!.Description.Should().Contain("\"Unix and C are the ultimate computer viruses.\"");
        result.Description.Should().Contain("Richard Gabriel");
        result.Description.Should().Contain("American computer scientist");
    }

    [Fact]
    public async Task EnrichAsync_NoAuthor_ReturnsQuoteOnlyDescription()
    {
        var chat = Substitute.For<IChatClient>();
        var enricher = new QuotesEnricher(chat, NullLogger<QuotesEnricher>.Instance);

        var result = await enricher.EnrichAsync(Sample(), Classification(null), default);

        result!.Description.Should().Contain("Unix and C");
        result.Description.Should().NotContain("**About");
        await chat.DidNotReceive().GetResponseAsync(
            Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnrichAsync_EmptyBio_OmitsBioSection()
    {
        var chat = Substitute.For<IChatClient>();
        chat.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions?>(), Arg.Any<CancellationToken>())
            .Returns(new ChatResponse(new ChatMessage(ChatRole.Assistant, "")));

        var enricher = new QuotesEnricher(chat, NullLogger<QuotesEnricher>.Instance);

        var result = await enricher.EnrichAsync(Sample(), Classification("Unknown Person"), default);

        result!.Description.Should().NotContain("**About");
    }

    [Fact]
    public async Task BucketName_IsQuotes()
    {
        var chat = Substitute.For<IChatClient>();
        var enricher = new QuotesEnricher(chat, NullLogger<QuotesEnricher>.Instance);
        enricher.BucketName.Should().Be("Quotes");
    }

    [Fact]
    public async Task EnrichAsync_TruncatesExcessivelyLongAuthorBeforeSendingToLlm()
    {
        IList<ChatMessage>? capturedMessages = null;
        var chat = Substitute.For<IChatClient>();
        chat.GetResponseAsync(
                Arg.Do<IEnumerable<ChatMessage>>(m => capturedMessages = m.ToList()),
                Arg.Any<ChatOptions?>(),
                Arg.Any<CancellationToken>())
            .Returns(new ChatResponse(new ChatMessage(ChatRole.Assistant, "bio")));

        var longAuthor = new string('x', 500);
        var enricher = new QuotesEnricher(chat, NullLogger<QuotesEnricher>.Instance);

        await enricher.EnrichAsync(Sample(), Classification(longAuthor), default);

        capturedMessages.Should().NotBeNull();
        var userMessage = capturedMessages!.Single(m => m.Role == ChatRole.User).Text;
        userMessage.Length.Should().BeLessThanOrEqualTo(120);
    }
}

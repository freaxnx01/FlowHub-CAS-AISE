using FlowHub.AI.Enrichers;
using FlowHub.Core.Classification;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;

namespace FlowHub.Web.ComponentTests.Classification;

public class ZitateEnricherTests
{
    private static Capture Sample() => new(
        Guid.NewGuid(), ChannelKind.Web,
        "\"Unix and C are the ultimate computer viruses.\", Richard Gabriel",
        DateTimeOffset.UtcNow, LifecycleStage.Raw, "Vikunja");

    private static ClassificationResult Classification(string? author) =>
        new(["quote"], "Vikunja", "Gabriel on Unix and C", "Zitate",
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

        var enricher = new ZitateEnricher(chat, NullLogger<ZitateEnricher>.Instance);

        var result = await enricher.EnrichAsync(Sample(), Classification("Richard Gabriel"), default);

        result!.Description.Should().Contain("\"Unix and C are the ultimate computer viruses.\"");
        result.Description.Should().Contain("Richard Gabriel");
        result.Description.Should().Contain("American computer scientist");
    }

    [Fact]
    public async Task EnrichAsync_NoAuthor_ReturnsQuoteOnlyDescription()
    {
        var chat = Substitute.For<IChatClient>();
        var enricher = new ZitateEnricher(chat, NullLogger<ZitateEnricher>.Instance);

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

        var enricher = new ZitateEnricher(chat, NullLogger<ZitateEnricher>.Instance);

        var result = await enricher.EnrichAsync(Sample(), Classification("Unknown Person"), default);

        result!.Description.Should().NotContain("**About");
    }

    [Fact]
    public async Task BucketName_IsZitate()
    {
        var chat = Substitute.For<IChatClient>();
        var enricher = new ZitateEnricher(chat, NullLogger<ZitateEnricher>.Instance);
        enricher.BucketName.Should().Be("Zitate");
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
        var enricher = new ZitateEnricher(chat, NullLogger<ZitateEnricher>.Instance);

        await enricher.EnrichAsync(Sample(), Classification(longAuthor), default);

        capturedMessages.Should().NotBeNull();
        var userMessage = capturedMessages!.Single(m => m.Role == ChatRole.User).Text;
        // The (untrusted) author is capped at 120 chars before being sent to the
        // bio LLM — the message also carries the quote, so assert on the author run.
        userMessage.Should().Contain(new string('x', 120));
        userMessage.Should().NotContain(new string('x', 121));
    }
}

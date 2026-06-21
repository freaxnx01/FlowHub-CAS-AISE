using System.Text.Json;
using FlowHub.AI;
using FlowHub.AI.Enrichers;
using FlowHub.Core.Classification;
using FlowHub.Core.Skills;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;

namespace FlowHub.Web.ComponentTests.Classification;

public class ClassifyAndEnrichPipelineTests
{
    private static ChatResponse JsonResponse(object payload) =>
        new(new ChatMessage(ChatRole.Assistant, JsonSerializer.Serialize(payload)));

    private static ChatResponse TextResponse(string text) =>
        new(new ChatMessage(ChatRole.Assistant, text));

    [Fact]
    public async Task RichardGabrielQuote_RoutesToZitateAndProducesBio()
    {
        // Catalog with both Inbox and Zitate
        var catalog = Substitute.For<IVikunjaProjectCatalog>();
        catalog.GetAsync(Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, int> { ["Inbox"] = 1, ["Zitate"] = 7 });

        // Same chat client handles both calls.
        // Call 1: classifier — JSON payload conforming to AiClassificationResponse.
        // Call 2: enricher — plain-text bio.
        var chat = Substitute.For<IChatClient>();
        chat.GetResponseAsync(
                Arg.Any<IEnumerable<ChatMessage>>(),
                Arg.Any<ChatOptions?>(),
                Arg.Any<CancellationToken>())
            .Returns(
                _ => JsonResponse(new
                {
                    tags = new[] { "quote", "computing" },
                    matched_skill = "Vikunja",
                    title = "Gabriel on Unix and C",
                    project = "Zitate",
                    entities = new Dictionary<string, string>
                    {
                        ["quote"] = "Unix and C are the ultimate computer viruses.",
                        ["author"] = "Richard Gabriel",
                    },
                }),
                _ => TextResponse("American computer scientist; co-author of the 'Worse is Better' essay."));

        var keyword = new KeywordClassifier();
        var classifier = new AiClassifier(chat, keyword,
            NullLogger<AiClassifier>.Instance, new ChatOptions(), catalog,
            new AiModelInfo("OpenRouter", "test-model"));

        var dispatcher = new EnricherDispatcher(
            new IEnricher[] { new ZitateEnricher(chat, NullLogger<ZitateEnricher>.Instance) },
            catalog,
            new VikunjaFallback("Inbox", 1),
            NullLogger<EnricherDispatcher>.Instance);

        var capture = new Capture(Guid.NewGuid(), ChannelKind.Web,
            "\"Unix and C are the ultimate computer viruses.\", Richard Gabriel",
            DateTimeOffset.UtcNow, LifecycleStage.Raw, null);

        var result = await classifier.ClassifyAsync(capture.Content, default);
        var (project, enrichment) = await dispatcher.DispatchAsync(capture, result, default);

        project.Should().Be("Zitate");
        enrichment.Should().NotBeNull();
        enrichment!.Description.Should().Contain("Unix and C")
                                          .And.Contain("Richard Gabriel")
                                          .And.Contain("computer scientist");
    }
}

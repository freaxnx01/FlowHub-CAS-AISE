using System.Text.Json;
using FlowHub.AI;
using FlowHub.Core.Classification;
using FlowHub.Core.Skills;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;

namespace FlowHub.Web.ComponentTests.Classification;

public class AiClassifierTraceTests
{
    private static ChatResponse JsonResponse(object payload, UsageDetails? usage = null) =>
        new(new ChatMessage(ChatRole.Assistant, JsonSerializer.Serialize(payload)))
        {
            Usage = usage,
        };

    private static IVikunjaProjectCatalog Catalog()
    {
        var catalog = Substitute.For<IVikunjaProjectCatalog>();
        catalog.GetAsync(Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, int> { ["Inbox"] = 1, ["Quotes"] = 7 });
        return catalog;
    }

    private static IChatClient ChatReturning(ChatResponse response)
    {
        var chat = Substitute.For<IChatClient>();
        chat.GetResponseAsync(
                Arg.Any<IEnumerable<ChatMessage>>(),
                Arg.Any<ChatOptions?>(),
                Arg.Any<CancellationToken>())
            .Returns(response);
        return chat;
    }

    [Fact]
    public async Task ClassifyAsync_OnSuccess_RecordsAiTraceWithProviderModelAndTokens()
    {
        var usage = new UsageDetails { InputTokenCount = 123, OutputTokenCount = 45 };
        var chat = ChatReturning(JsonResponse(new
        {
            tags = new[] { "quote", "computing" },
            matched_skill = "Vikunja",
            title = "Gabriel on Unix and C",
            project = "Quotes",
            entities = new Dictionary<string, string>
            {
                ["quote"] = "Unix and C are the ultimate computer viruses.",
                ["author"] = "Richard Gabriel",
            },
        }, usage));

        var classifier = new AiClassifier(
            chat,
            new KeywordClassifier(),
            NullLogger<AiClassifier>.Instance,
            new ChatOptions(),
            Catalog(),
            new AiModelInfo("OpenRouter", "google/gemma-4-31b-it:free"));

        var result = await classifier.ClassifyAsync("\"Unix and C ...\", Richard Gabriel", default);

        result.Trace.Should().NotBeNull();
        result.Trace!.Kind.Should().Be(ClassifierKind.Ai);
        result.Trace.Provider.Should().Be("OpenRouter");
        result.Trace.Model.Should().Be("google/gemma-4-31b-it:free");
        result.Trace.PromptTokens.Should().Be(123);
        result.Trace.CompletionTokens.Should().Be(45);
        result.Trace.LatencyMs.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task ClassifyAsync_OnSchemaViolation_FallsBackToKeywordTrace()
    {
        // Invalid MatchedSkill ("Nonsense") forces the schema_violation fallback.
        var chat = ChatReturning(JsonResponse(new
        {
            tags = new[] { "x" },
            matched_skill = "Nonsense",
            title = "t",
        }));

        var classifier = new AiClassifier(
            chat,
            new KeywordClassifier(),
            NullLogger<AiClassifier>.Instance,
            new ChatOptions(),
            Catalog(),
            new AiModelInfo("OpenRouter", "google/gemma-4-31b-it:free"));

        var result = await classifier.ClassifyAsync("some content", default);

        result.Trace.Should().NotBeNull();
        result.Trace!.Kind.Should().Be(ClassifierKind.Keyword);
        result.Trace.Provider.Should().BeNull();
        result.Trace.Model.Should().BeNull();
        result.Trace.PromptTokens.Should().BeNull();
        result.Trace.CompletionTokens.Should().BeNull();
    }
}

using System.Text.Json;
using FlowHub.AI;
using FlowHub.Core.Classification;
using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace FlowHub.Web.ComponentTests.Ai;

public sealed class AiClassifierTests
{
    private readonly IChatClient _chat = Substitute.For<IChatClient>();
    private readonly IClassifier _keyword = Substitute.For<IClassifier>();
    private readonly FakeLogger<AiClassifier> _log = new();
    private readonly ChatOptions _opts = new() { MaxOutputTokens = 300, Temperature = 0.2f };

    private AiClassifier Sut() => new(_chat, _keyword, _log, _opts);

    private static ChatResponse JsonResponse(object payload) =>
        new(new ChatMessage(ChatRole.Assistant, JsonSerializer.Serialize(payload)));

    [Fact]
    public async Task ClassifyAsync_AiSucceedsWithValidSchema_ReturnsAiResult()
    {
        _chat.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions?>(), Arg.Any<CancellationToken>())
             .Returns(JsonResponse(new
             {
                 tags = new[] { "link", "article" },
                 matched_skill = "Wallabag",
                 title = "Saving an article for later",
             }));

        var result = await Sut().ClassifyAsync("https://example.com/article", default);

        result.MatchedSkill.Should().Be("Wallabag");
        result.Tags.Should().BeEquivalentTo(new[] { "link", "article" });
        result.Title.Should().Be("Saving an article for later");
        await _keyword.DidNotReceive().ClassifyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ClassifyAsync_ForwardsCancellationTokenToChatClient()
    {
        using var cts = new CancellationTokenSource();
        _chat.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions?>(), Arg.Any<CancellationToken>())
             .Returns(JsonResponse(new { tags = new[] { "x" }, matched_skill = "", title = (string?)null }));

        await Sut().ClassifyAsync("anything", cts.Token);

        await _chat.Received(1).GetResponseAsync(
            Arg.Any<IEnumerable<ChatMessage>>(),
            Arg.Any<ChatOptions?>(),
            cts.Token);
    }

    [Fact]
    public async Task ClassifyAsync_PassesMaxOutputTokens300_ToChatClient()
    {
        _chat.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions?>(), Arg.Any<CancellationToken>())
             .Returns(JsonResponse(new { tags = new[] { "x" }, matched_skill = "", title = (string?)null }));

        await Sut().ClassifyAsync("anything", default);

        await _chat.Received(1).GetResponseAsync(
            Arg.Any<IEnumerable<ChatMessage>>(),
            Arg.Is<ChatOptions?>(o => o != null && o.MaxOutputTokens == 300),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ClassifyAsync_PassesTemperature02_ToChatClient()
    {
        _chat.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions?>(), Arg.Any<CancellationToken>())
             .Returns(JsonResponse(new { tags = new[] { "x" }, matched_skill = "", title = (string?)null }));

        await Sut().ClassifyAsync("anything", default);

        await _chat.Received(1).GetResponseAsync(
            Arg.Any<IEnumerable<ChatMessage>>(),
            Arg.Is<ChatOptions?>(o => o != null && Math.Abs((double)(o.Temperature ?? 0f) - 0.2) < 0.0001),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ClassifyAsync_HttpRequestException_FallsBackToKeyword()
    {
        _chat.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions?>(), Arg.Any<CancellationToken>())
             .ThrowsAsync(new HttpRequestException("network down"));
        _keyword.ClassifyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(new ClassificationResult(["unsorted"], string.Empty));

        var result = await Sut().ClassifyAsync("anything", default);

        result.Should().BeEquivalentTo(new ClassificationResult(["unsorted"], string.Empty));
        await _keyword.Received(1).ClassifyAsync("anything", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ClassifyAsync_TaskCanceledException_FallsBackToKeyword()
    {
        _chat.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions?>(), Arg.Any<CancellationToken>())
             .ThrowsAsync(new TaskCanceledException("HttpClient timeout"));
        _keyword.ClassifyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(new ClassificationResult(["unsorted"], string.Empty));

        var result = await Sut().ClassifyAsync("anything", default);

        result.MatchedSkill.Should().BeEmpty();
        await _keyword.Received(1).ClassifyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ClassifyAsync_JsonException_FallsBackToKeyword()
    {
        _chat.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions?>(), Arg.Any<CancellationToken>())
             .Returns(new ChatResponse(new ChatMessage(ChatRole.Assistant, "this is not JSON")));
        _keyword.ClassifyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(new ClassificationResult(["unsorted"], string.Empty));

        var result = await Sut().ClassifyAsync("anything", default);

        result.MatchedSkill.Should().BeEmpty();
        await _keyword.Received(1).ClassifyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ClassifyAsync_MatchedSkillOutsideAllowedSet_FallsBackAndLogsSchemaViolation()
    {
        _chat.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions?>(), Arg.Any<CancellationToken>())
             .Returns(JsonResponse(new { tags = new[] { "x" }, matched_skill = "Bogus", title = "t" }));
        _keyword.ClassifyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(new ClassificationResult(["unsorted"], string.Empty));

        var result = await Sut().ClassifyAsync("anything", default);

        result.MatchedSkill.Should().BeEmpty();
        _log.Records.Should().ContainSingle(r => r.EventId.Id == 3010 && r.Message.Contains("schema_violation"));
    }

    [Fact]
    public async Task ClassifyAsync_GenericException_FallsBackToKeyword()
    {
        _chat.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions?>(), Arg.Any<CancellationToken>())
             .ThrowsAsync(new InvalidOperationException("anything else"));
        _keyword.ClassifyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(new ClassificationResult(["unsorted"], string.Empty));

        var result = await Sut().ClassifyAsync("anything", default);

        result.MatchedSkill.Should().BeEmpty();
        await _keyword.Received(1).ClassifyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ClassifyAsync_OnFallback_LogsEventId3010WithExceptionTypeAndDuration()
    {
        _chat.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions?>(), Arg.Any<CancellationToken>())
             .ThrowsAsync(new HttpRequestException("oops"));
        _keyword.ClassifyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(new ClassificationResult(["unsorted"], string.Empty));

        await Sut().ClassifyAsync("anything", default);

        var record = _log.Records.Should().ContainSingle(r => r.EventId.Id == 3010).Subject;
        record.Level.Should().Be(LogLevel.Warning);
        record.Message.Should().Contain(nameof(HttpRequestException));
        record.Message.Should().MatchRegex(@"duration_ms=\d+");
    }

    // ---------------------------------------------------------------------------
    // Test infrastructure — kept in the same file for locality
    // ---------------------------------------------------------------------------

    internal sealed record LogRecord(LogLevel Level, EventId EventId, string Message);

    internal sealed class FakeLogger<T> : ILogger<T>
    {
        public List<LogRecord> Records { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Records.Add(new LogRecord(logLevel, eventId, formatter(state, exception)));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }
}

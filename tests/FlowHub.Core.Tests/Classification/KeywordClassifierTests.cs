using FlowHub.Core.Classification;
using FluentAssertions;

namespace FlowHub.Core.Tests.Classification;

/// <summary>
/// Behavioural tests for <see cref="KeywordClassifier"/>'s routing logic.
/// Companion to <see cref="KeywordClassifierTraceTests"/>, which only asserts
/// the trace shape. These tests target the surviving mutants from #96 by pinning
/// the exact labels, skill names, scheme set, and keyword set.
/// </summary>
public sealed class KeywordClassifierTests
{
    private readonly KeywordClassifier _sut = new();

    // --- URL detection: positive cases ---------------------------------------

    [Theory]
    [InlineData("https://example.com")]
    [InlineData("http://example.com")]
    [InlineData("https://example.com/path?q=1")]
    [InlineData("  https://example.com  ")] // surrounding whitespace is trimmed before parsing
    public async Task ClassifyAsync_UrlContent_RoutesToWallabagLink(string content)
    {
        var result = await _sut.ClassifyAsync(content, default);

        result.Tags.Should().ContainSingle().Which.Should().Be("link");
        result.MatchedSkill.Should().Be("Wallabag");
    }

    // --- URL detection: only http/https route to Wallabag --------------------

    [Theory]
    [InlineData("ftp://example.com/file.txt")]
    [InlineData("file:///etc/hosts")]
    [InlineData("mailto:alice@example.com")]
    [InlineData("ssh://host")]
    public async Task ClassifyAsync_NonHttpUriScheme_IsUnsorted(string content)
    {
        var result = await _sut.ClassifyAsync(content, default);

        result.Tags.Should().ContainSingle().Which.Should().Be("unsorted");
        result.MatchedSkill.Should().BeEmpty();
    }

    // --- Keyword detection: positive cases -----------------------------------

    [Theory]
    [InlineData("todo: buy milk")]
    [InlineData("TODO: buy milk")]              // case-insensitive
    [InlineData("remember the task tomorrow")]
    [InlineData("Tomorrow's TASK list")]         // case-insensitive
    public async Task ClassifyAsync_TodoOrTaskKeyword_RoutesToVikunjaTask(string content)
    {
        var result = await _sut.ClassifyAsync(content, default);

        result.Tags.Should().ContainSingle().Which.Should().Be("task");
        result.MatchedSkill.Should().Be("Vikunja");
    }

    // --- Keyword detection: negative cases (no false positives) --------------

    [Theory]
    [InlineData("random musings")]
    [InlineData("a sentence without the magic words")]
    [InlineData("")]
    public async Task ClassifyAsync_NeitherUrlNorKeyword_IsUnsorted(string content)
    {
        var result = await _sut.ClassifyAsync(content, default);

        result.Tags.Should().ContainSingle().Which.Should().Be("unsorted");
        result.MatchedSkill.Should().BeEmpty();
    }

    // --- Precedence: URL beats TODO keyword ----------------------------------

    [Fact]
    public async Task ClassifyAsync_UrlContainingTodoKeyword_StillRoutesAsLink()
    {
        // "todo" appears inside a URL — URL detection runs first.
        var result = await _sut.ClassifyAsync("https://example.com/todo/123", default);

        result.Tags.Should().ContainSingle().Which.Should().Be("link");
        result.MatchedSkill.Should().Be("Wallabag");
    }

    // --- Null-guard ----------------------------------------------------------

    [Fact]
    public async Task ClassifyAsync_NullContent_ThrowsArgumentNullException()
    {
        Func<Task> act = () => _sut.ClassifyAsync(null!, default);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}

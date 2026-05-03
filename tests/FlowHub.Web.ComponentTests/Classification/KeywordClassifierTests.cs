using FlowHub.Core.Classification;
using FluentAssertions;

namespace FlowHub.Web.ComponentTests.Classification;

public sealed class KeywordClassifierTests
{
    private readonly KeywordClassifier _sut = new();

    [Fact]
    public async Task ClassifyAsync_UrlContent_RoutesToWallabag()
    {
        var result = await _sut.ClassifyAsync("https://example.com/article", default);

        result.MatchedSkill.Should().Be("Wallabag");
        result.Tags.Should().ContainSingle().Which.Should().Be("link");
    }

    [Fact]
    public async Task ClassifyAsync_TodoContent_RoutesToVikunja()
    {
        var result = await _sut.ClassifyAsync("todo: buy milk", default);

        result.MatchedSkill.Should().Be("Vikunja");
        result.Tags.Should().ContainSingle().Which.Should().Be("task");
    }

    [Fact]
    public async Task ClassifyAsync_TaskWordCaseInsensitive_RoutesToVikunja()
    {
        var result = await _sut.ClassifyAsync("TASK list for tomorrow", default);

        result.MatchedSkill.Should().Be("Vikunja");
    }

    [Fact]
    public async Task ClassifyAsync_PlainText_ReturnsEmptySkill()
    {
        var result = await _sut.ClassifyAsync("just some random sentence", default);

        result.MatchedSkill.Should().BeEmpty();
        result.Tags.Should().ContainSingle().Which.Should().Be("unsorted");
    }

    [Fact]
    public async Task ClassifyAsync_AnyContent_KeywordClassifierReturnsNullTitle()
    {
        var result = await _sut.ClassifyAsync("https://example.com/article", default);

        result.Title.Should().BeNull();
    }
}

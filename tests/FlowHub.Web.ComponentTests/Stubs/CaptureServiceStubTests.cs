using FlowHub.Web.Stubs;

namespace FlowHub.Web.ComponentTests.Stubs;

public class CaptureServiceStubTests
{
    [Fact]
    public async Task GetRecentAsync_RespectsCount()
    {
        var sut = new CaptureServiceStub();

        var recent = await sut.GetRecentAsync(5);

        recent.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetFailureCountsAsync_ReturnsBiasedSeed_WithOrphansAndUnhandled()
    {
        var sut = new CaptureServiceStub();

        var counts = await sut.GetFailureCountsAsync();

        counts.OrphanCount.Should().BeGreaterThan(0, "the seed has at least one orphan");
        counts.UnhandledCount.Should().BeGreaterThan(0, "the seed has at least one unhandled");
        counts.AnyFailures.Should().BeTrue();
    }

    [Fact]
    public async Task SubmitAsync_AppendsRawCapture_AndReturnsIt()
    {
        var sut = new CaptureServiceStub();
        var before = await sut.GetRecentAsync(100);

        var submitted = await sut.SubmitAsync("https://example.com/new-thing", ChannelKind.Web);

        submitted.Stage.Should().Be(LifecycleStage.Raw);
        submitted.Source.Should().Be(ChannelKind.Web);
        submitted.Content.Should().Be("https://example.com/new-thing");

        var after = await sut.GetRecentAsync(100);
        after.Should().HaveCount(before.Count + 1);
        after.Should().Contain(c => c.Id == submitted.Id);
    }

    [Fact]
    public async Task SubmitAsync_RejectsEmptyContent()
    {
        var sut = new CaptureServiceStub();

        var act = () => sut.SubmitAsync("   ", ChannelKind.Web);

        await act.Should().ThrowAsync<ArgumentException>();
    }
}

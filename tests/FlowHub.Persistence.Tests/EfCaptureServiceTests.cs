using FlowHub.Core.Captures;
using FlowHub.Core.Events;
using FlowHub.Persistence;
using MassTransit;
using NSubstitute;
using FluentAssertions;

namespace FlowHub.Persistence.Tests;

public sealed class EfCaptureServiceTests
{
    private static (ICaptureRepository repo, EfCaptureService sut, IPublishEndpoint ep) Build()
    {
        var ep = Substitute.For<IPublishEndpoint>();
        var repo = Substitute.For<ICaptureRepository>();
        var storage = Substitute.For<IAttachmentStorage>();
        return (repo, new EfCaptureService(repo, ep, storage), ep);
    }

    private static Capture MakeCapture(Guid? id = null, LifecycleStage stage = LifecycleStage.Raw,
        string? matchedSkill = null, string? failureReason = null) =>
        new(id ?? Guid.NewGuid(), ChannelKind.Web, "content", DateTimeOffset.UtcNow, stage, matchedSkill, failureReason);

    [Fact]
    public async Task SubmitAsync_RejectsEmptyContent()
    {
        var (_, sut, _) = Build();
        var act = () => sut.SubmitAsync("   ", ChannelKind.Web);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task SubmitAsync_PublishesCaptureCreated()
    {
        var (repo, sut, ep) = Build();
        var returned = MakeCapture();
        repo.AddAsync(Arg.Any<Capture>(), Arg.Any<CancellationToken>()).Returns(returned);

        var result = await sut.SubmitAsync("https://example.com", ChannelKind.Web);

        result.Stage.Should().Be(LifecycleStage.Raw);
        await ep.Received(1).Publish(
            Arg.Is<CaptureCreated>(m => m.CaptureId == returned.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByIdAsync_DelegatesToRepository()
    {
        var (repo, sut, _) = Build();
        var id = Guid.NewGuid();
        var capture = MakeCapture(id);
        repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(capture);

        var result = await sut.GetByIdAsync(id);

        result.Should().Be(capture);
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        var (repo, sut, _) = Build();
        repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Capture?)null);

        var result = await sut.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetRecentAsync_DelegatesToRepository()
    {
        var (repo, sut, _) = Build();
        IReadOnlyList<Capture> snapshot = [MakeCapture(), MakeCapture()];
        repo.GetRecentAsync(2, Arg.Any<CancellationToken>()).Returns(snapshot);

        var result = await sut.GetRecentAsync(2);

        result.Should().BeEquivalentTo(snapshot);
    }

    [Fact]
    public async Task GetFailureCountsAsync_DelegatesToRepository()
    {
        var (repo, sut, _) = Build();
        var counts = new FailureCounts(2, 1);
        repo.GetFailureCountsAsync(Arg.Any<CancellationToken>()).Returns(counts);

        var result = await sut.GetFailureCountsAsync();

        result.Should().Be(counts);
    }

    [Fact]
    public async Task ListAsync_DelegatesToRepository()
    {
        var (repo, sut, _) = Build();
        var filter = new CaptureFilter(null, null, 20, null);
        var page = new CapturePage([], null);
        repo.ListAsync(filter, Arg.Any<CancellationToken>()).Returns(page);

        var result = await sut.ListAsync(filter);

        result.Should().Be(page);
    }

    [Fact]
    public async Task MarkClassifiedAsync_UpdatesStageAndSkillAndTitle()
    {
        var (repo, sut, _) = Build();
        var id = Guid.NewGuid();
        repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(MakeCapture(id));

        await sut.MarkClassifiedAsync(id, "Wallabag", "Some Title");

        await repo.Received(1).UpdateAsync(
            Arg.Is<Capture>(c =>
                c.Stage == LifecycleStage.Classified &&
                c.MatchedSkill == "Wallabag" &&
                c.Title == "Some Title"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkRoutedAsync_FlipsStageToRouted()
    {
        var (repo, sut, _) = Build();
        var id = Guid.NewGuid();
        repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(MakeCapture(id, LifecycleStage.Classified, "Wallabag"));

        await sut.MarkRoutedAsync(id);

        await repo.Received(1).UpdateAsync(
            Arg.Is<Capture>(c => c.Stage == LifecycleStage.Routed),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkCompletedAsync_PersistsExternalRef()
    {
        var (repo, sut, _) = Build();
        var id = Guid.NewGuid();
        repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(MakeCapture(id, LifecycleStage.Routed, "Wallabag"));

        await sut.MarkCompletedAsync(id, "wal-42");

        await repo.Received(1).UpdateAsync(
            Arg.Is<Capture>(c => c.Stage == LifecycleStage.Completed && c.ExternalRef == "wal-42"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkOrphanAsync_PersistsFailureReason()
    {
        var (repo, sut, _) = Build();
        var id = Guid.NewGuid();
        repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(MakeCapture(id));

        await sut.MarkOrphanAsync(id, "no skill matched");

        await repo.Received(1).UpdateAsync(
            Arg.Is<Capture>(c => c.Stage == LifecycleStage.Orphan && c.FailureReason == "no skill matched"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkUnhandledAsync_PersistsFailureReason()
    {
        var (repo, sut, _) = Build();
        var id = Guid.NewGuid();
        repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(MakeCapture(id, LifecycleStage.Classified, "Wallabag"));

        await sut.MarkUnhandledAsync(id, "wallabag 503");

        await repo.Received(1).UpdateAsync(
            Arg.Is<Capture>(c => c.Stage == LifecycleStage.Unhandled && c.FailureReason == "wallabag 503"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResetForRetryAsync_ResetsStageAndClearsFailureReason()
    {
        var (repo, sut, _) = Build();
        var id = Guid.NewGuid();
        repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(MakeCapture(id, LifecycleStage.Orphan, null, "no skill matched"));

        await sut.ResetForRetryAsync(id);

        await repo.Received(1).UpdateAsync(
            Arg.Is<Capture>(c => c.Stage == LifecycleStage.Raw && c.FailureReason == null),
            Arg.Any<CancellationToken>());
    }
}

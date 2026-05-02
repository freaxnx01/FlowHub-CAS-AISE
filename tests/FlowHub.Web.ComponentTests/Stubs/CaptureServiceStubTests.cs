using FlowHub.Core.Captures;
using FlowHub.Core.Events;
using FlowHub.Web.Stubs;
using MassTransit;

namespace FlowHub.Web.ComponentTests.Stubs;

public sealed class CaptureServiceStubTests
{
    // ── helpers ─────────────────────────────────────────────────────────────

    /// <summary>
    /// No-op <see cref="IPublishEndpoint"/> for tests that do not need to verify
    /// that a message was published.
    /// </summary>
    private sealed class NoopPublishEndpoint : IPublishEndpoint
    {
        public static readonly NoopPublishEndpoint Instance = new();

        public ConnectHandle ConnectPublishObserver(IPublishObserver observer) => new NoopHandle();

        public Task Publish<T>(T message, CancellationToken cancellationToken = default) where T : class =>
            Task.CompletedTask;

        public Task Publish<T>(T message, IPipe<PublishContext<T>> pipe, CancellationToken cancellationToken = default) where T : class =>
            Task.CompletedTask;

        public Task Publish<T>(T message, IPipe<PublishContext> pipe, CancellationToken cancellationToken = default) where T : class =>
            Task.CompletedTask;

        public Task Publish(object message, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task Publish(object message, Type messageType, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task Publish(object message, IPipe<PublishContext> pipe, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task Publish(object message, Type messageType, IPipe<PublishContext> pipe, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task Publish<T>(object values, CancellationToken cancellationToken = default) where T : class => Task.CompletedTask;

        public Task Publish<T>(object values, IPipe<PublishContext<T>> pipe, CancellationToken cancellationToken = default) where T : class => Task.CompletedTask;

        public Task Publish<T>(object values, IPipe<PublishContext> pipe, CancellationToken cancellationToken = default) where T : class => Task.CompletedTask;

        private sealed class NoopHandle : ConnectHandle
        {
            public void Disconnect() { }
            public void Dispose() { }
        }
    }

    // ── SubmitAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task SubmitAsync_NewContent_PublishesCaptureCreated()
    {
        CaptureCreated? published = null;
        var endpoint = Substitute.For<IPublishEndpoint>();
        endpoint
            .Publish(Arg.Do<CaptureCreated>(m => published = m), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = new CaptureServiceStub(endpoint);

        var capture = await sut.SubmitAsync("hello", ChannelKind.Web, default);

        await endpoint.Received(1).Publish(Arg.Any<CaptureCreated>(), Arg.Any<CancellationToken>());
        published.Should().NotBeNull();
        published!.CaptureId.Should().Be(capture.Id);
        published.Content.Should().Be("hello");
        published.Source.Should().Be(ChannelKind.Web);
    }

    [Fact]
    public async Task SubmitAsync_AppendsRawCapture_AndReturnsIt()
    {
        var sut = new CaptureServiceStub(NoopPublishEndpoint.Instance);
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
        var sut = new CaptureServiceStub(NoopPublishEndpoint.Instance);

        var act = () => sut.SubmitAsync("   ", ChannelKind.Web);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    // ── GetRecentAsync / GetFailureCountsAsync (Block-2 regression) ──────────

    [Fact]
    public async Task GetRecentAsync_RespectsCount()
    {
        var sut = new CaptureServiceStub(NoopPublishEndpoint.Instance);

        var recent = await sut.GetRecentAsync(5);

        recent.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetFailureCountsAsync_ReturnsBiasedSeed_WithOrphansAndUnhandled()
    {
        var sut = new CaptureServiceStub(NoopPublishEndpoint.Instance);

        var counts = await sut.GetFailureCountsAsync();

        counts.OrphanCount.Should().BeGreaterThan(0, "the seed has at least one orphan");
        counts.UnhandledCount.Should().BeGreaterThan(0, "the seed has at least one unhandled");
        counts.AnyFailures.Should().BeTrue();
    }

    // ── MarkClassifiedAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task MarkClassifiedAsync_UpdatesCaptureStageAndSkill()
    {
        var sut = new CaptureServiceStub(NoopPublishEndpoint.Instance);
        var capture = await sut.SubmitAsync("hello", ChannelKind.Web, default);

        await sut.MarkClassifiedAsync(capture.Id, "Wallabag", default);

        var updated = await sut.GetByIdAsync(capture.Id, default);
        updated!.Stage.Should().Be(LifecycleStage.Classified);
        updated.MatchedSkill.Should().Be("Wallabag");
    }

    [Fact]
    public async Task MarkClassifiedAsync_UnknownId_Throws()
    {
        var sut = new CaptureServiceStub(NoopPublishEndpoint.Instance);

        var act = () => sut.MarkClassifiedAsync(Guid.NewGuid(), "Wallabag", default);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // ── MarkRoutedAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task MarkRoutedAsync_FlipsToRouted()
    {
        var sut = new CaptureServiceStub(NoopPublishEndpoint.Instance);
        var capture = await sut.SubmitAsync("hello", ChannelKind.Web, default);

        await sut.MarkRoutedAsync(capture.Id, default);

        (await sut.GetByIdAsync(capture.Id, default))!.Stage.Should().Be(LifecycleStage.Routed);
    }

    // ── MarkOrphanAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task MarkOrphanAsync_FlipsToOrphanWithReason()
    {
        var sut = new CaptureServiceStub(NoopPublishEndpoint.Instance);
        var capture = await sut.SubmitAsync("hello", ChannelKind.Web, default);

        await sut.MarkOrphanAsync(capture.Id, "no skill matched", default);

        var updated = (await sut.GetByIdAsync(capture.Id, default))!;
        updated.Stage.Should().Be(LifecycleStage.Orphan);
        updated.FailureReason.Should().Be("no skill matched");
    }

    // ── MarkUnhandledAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task MarkUnhandledAsync_FlipsToUnhandledWithReason()
    {
        var sut = new CaptureServiceStub(NoopPublishEndpoint.Instance);
        var capture = await sut.SubmitAsync("hello", ChannelKind.Web, default);

        await sut.MarkUnhandledAsync(capture.Id, "integration down", default);

        var updated = (await sut.GetByIdAsync(capture.Id, default))!;
        updated.Stage.Should().Be(LifecycleStage.Unhandled);
        updated.FailureReason.Should().Be("integration down");
    }
}

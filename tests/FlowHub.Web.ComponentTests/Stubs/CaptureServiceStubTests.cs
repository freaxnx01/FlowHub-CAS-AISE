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

    // ── ListAsync ──

    [Fact]
    public async Task ListAsync_NoFilter_ReturnsAllOrderedByCreatedAtDesc()
    {
        var sut = new CaptureServiceStub(NoopPublishEndpoint.Instance);

        var page = await sut.ListAsync(new CaptureFilter(null, null, Limit: 50, Cursor: null), default);

        page.Items.Should().HaveCountGreaterThanOrEqualTo(12);
        page.Items.Should().BeInDescendingOrder(c => c.CreatedAt);
    }

    [Fact]
    public async Task ListAsync_LimitTwo_ReturnsTwoItemsAndNextCursor()
    {
        var sut = new CaptureServiceStub(NoopPublishEndpoint.Instance);

        var page = await sut.ListAsync(new CaptureFilter(null, null, Limit: 2, Cursor: null), default);

        page.Items.Should().HaveCount(2);
        page.Next.Should().NotBeNull();
        page.Next!.CreatedAt.Should().Be(page.Items[1].CreatedAt);
        page.Next.Id.Should().Be(page.Items[1].Id);
    }

    [Fact]
    public async Task ListAsync_WithCursor_ReturnsItemsStrictlyAfterCursor()
    {
        var sut = new CaptureServiceStub(NoopPublishEndpoint.Instance);
        var firstPage = await sut.ListAsync(new CaptureFilter(null, null, Limit: 2, Cursor: null), default);

        var secondPage = await sut.ListAsync(
            new CaptureFilter(null, null, Limit: 2, Cursor: firstPage.Next), default);

        secondPage.Items.Should().NotBeEmpty();
        secondPage.Items.Select(c => c.Id).Should().NotIntersectWith(firstPage.Items.Select(c => c.Id));
    }

    [Fact]
    public async Task ListAsync_StageOrphan_ReturnsOnlyOrphanCaptures()
    {
        var sut = new CaptureServiceStub(NoopPublishEndpoint.Instance);

        var page = await sut.ListAsync(
            new CaptureFilter(Stages: new[] { LifecycleStage.Orphan }, null, Limit: 50, Cursor: null), default);

        page.Items.Should().NotBeEmpty();
        page.Items.Should().OnlyContain(c => c.Stage == LifecycleStage.Orphan);
    }

    // ── ResetForRetryAsync ──

    [Fact]
    public async Task ResetForRetryAsync_OrphanCapture_ResetsToRawAndClearsFailureReason()
    {
        var sut = new CaptureServiceStub(NoopPublishEndpoint.Instance);
        var capture = await sut.SubmitAsync("hello", ChannelKind.Web, default);
        await sut.MarkOrphanAsync(capture.Id, "no skill matched", default);

        await sut.ResetForRetryAsync(capture.Id, default);

        var updated = await sut.GetByIdAsync(capture.Id, default);
        updated!.Stage.Should().Be(LifecycleStage.Raw);
        updated.FailureReason.Should().BeNull();
    }

    [Fact]
    public async Task ResetForRetryAsync_UnknownId_Throws()
    {
        var sut = new CaptureServiceStub(NoopPublishEndpoint.Instance);

        Func<Task> act = () => sut.ResetForRetryAsync(Guid.NewGuid(), default);
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // ── MarkClassifiedAsync — Title forwarding (Beta MVP) ────────────────────

    [Fact]
    public async Task MarkClassifiedAsync_WithTitle_PersistsTitleOnCapture()
    {
        var sut = new CaptureServiceStub(NoopPublishEndpoint.Instance);
        var capture = await sut.SubmitAsync("https://example.com", ChannelKind.Web);

        await sut.MarkClassifiedAsync(capture.Id, "Wallabag", "Hexagonal architecture", default);

        var updated = await sut.GetByIdAsync(capture.Id);
        updated!.Stage.Should().Be(LifecycleStage.Classified);
        updated.MatchedSkill.Should().Be("Wallabag");
        updated.Title.Should().Be("Hexagonal architecture");
    }

    // ── MarkCompletedAsync (Beta MVP) ────────────────────────────────────────

    [Fact]
    public async Task MarkCompletedAsync_WithExternalRef_TransitionsToCompletedAndPersistsRef()
    {
        var sut = new CaptureServiceStub(NoopPublishEndpoint.Instance);
        var capture = await sut.SubmitAsync("https://example.com", ChannelKind.Web);
        await sut.MarkClassifiedAsync(capture.Id, "Wallabag", "Title", default);
        await sut.MarkRoutedAsync(capture.Id, default);

        await sut.MarkCompletedAsync(capture.Id, externalRef: "wal-42", default);

        var updated = await sut.GetByIdAsync(capture.Id);
        updated!.Stage.Should().Be(LifecycleStage.Completed);
        updated.ExternalRef.Should().Be("wal-42");
    }

    [Fact]
    public async Task MarkCompletedAsync_UnknownId_Throws()
    {
        var sut = new CaptureServiceStub(NoopPublishEndpoint.Instance);

        var act = () => sut.MarkCompletedAsync(Guid.NewGuid(), externalRef: null, default);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // ── Capture record shape (Beta MVP) ──────────────────────────────────────

    [Fact]
    public void Capture_RecordShape_HasOptionalTitleAndExternalRef()
    {
        var capture = new Capture(
            Id: Guid.NewGuid(),
            Source: ChannelKind.Web,
            Content: "https://example.com",
            CreatedAt: DateTimeOffset.UtcNow,
            Stage: LifecycleStage.Completed,
            MatchedSkill: "Wallabag",
            FailureReason: null,
            Title: "Example",
            ExternalRef: "wal-42");

        capture.Title.Should().Be("Example");
        capture.ExternalRef.Should().Be("wal-42");
    }
}

using FlowHub.Core.Captures;
using FlowHub.Core.Events;
using FlowHub.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace FlowHub.Persistence.Tests;

public sealed class EfCaptureServiceTests
{
    private static (FlowHubDbContext db, EfCaptureService sut, IPublishEndpoint endpoint) Build()
    {
        var endpoint = Substitute.For<IPublishEndpoint>();
        var options = new DbContextOptionsBuilder<FlowHubDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        var db = new FlowHubDbContext(options);
        return (db, new EfCaptureService(db, endpoint), endpoint);
    }

    [Fact]
    public async Task SubmitAsync_AppendsRawCaptureAndPublishesCaptureCreated()
    {
        var (db, sut, endpoint) = Build();

        var capture = await sut.SubmitAsync("https://example.com", ChannelKind.Web);

        capture.Stage.Should().Be(LifecycleStage.Raw);
        capture.Content.Should().Be("https://example.com");
        capture.Source.Should().Be(ChannelKind.Web);

        var stored = await db.Captures.SingleAsync(c => c.Id == capture.Id);
        stored.Stage.Should().Be(nameof(LifecycleStage.Raw));
        stored.Source.Should().Be(nameof(ChannelKind.Web));

        await endpoint.Received(1).Publish(
            Arg.Is<CaptureCreated>(m => m.CaptureId == capture.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SubmitAsync_RejectsEmptyContent()
    {
        var (_, sut, _) = Build();

        var act = () => sut.SubmitAsync("   ", ChannelKind.Web);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetByIdAsync_KnownId_ReturnsCapture()
    {
        var (_, sut, _) = Build();
        var submitted = await sut.SubmitAsync("hello", ChannelKind.Web);

        var found = await sut.GetByIdAsync(submitted.Id);

        found.Should().NotBeNull();
        found!.Id.Should().Be(submitted.Id);
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        var (_, sut, _) = Build();

        var found = await sut.GetByIdAsync(Guid.NewGuid());

        found.Should().BeNull();
    }

    [Fact]
    public async Task GetRecentAsync_OrdersByCreatedAtDescending()
    {
        var (db, sut, _) = Build();
        // Seed three with explicit timestamps.
        db.Captures.AddRange(
            new() { Id = Guid.NewGuid(), Content = "older",   Source = "Web", Stage = "Raw", CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-30) },
            new() { Id = Guid.NewGuid(), Content = "middle",  Source = "Web", Stage = "Raw", CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-10) },
            new() { Id = Guid.NewGuid(), Content = "newest",  Source = "Web", Stage = "Raw", CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();

        var recent = await sut.GetRecentAsync(10);

        recent.Should().HaveCount(3);
        recent.Select(c => c.Content).Should().ContainInOrder("newest", "middle", "older");
    }

    [Fact]
    public async Task MarkClassifiedAsync_PersistsTitleAndSkill()
    {
        var (_, sut, _) = Build();
        var capture = await sut.SubmitAsync("https://example.com", ChannelKind.Web);

        await sut.MarkClassifiedAsync(capture.Id, "Wallabag", "Hexagonal architecture");

        var updated = await sut.GetByIdAsync(capture.Id);
        updated!.Stage.Should().Be(LifecycleStage.Classified);
        updated.MatchedSkill.Should().Be("Wallabag");
        updated.Title.Should().Be("Hexagonal architecture");
    }

    [Fact]
    public async Task MarkRoutedAsync_FlipsStageToRouted()
    {
        var (_, sut, _) = Build();
        var capture = await sut.SubmitAsync("https://example.com", ChannelKind.Web);
        await sut.MarkClassifiedAsync(capture.Id, "Wallabag", "title");

        await sut.MarkRoutedAsync(capture.Id);

        var updated = await sut.GetByIdAsync(capture.Id);
        updated!.Stage.Should().Be(LifecycleStage.Routed);
    }

    [Fact]
    public async Task MarkCompletedAsync_PersistsExternalRefAndStage()
    {
        var (_, sut, _) = Build();
        var capture = await sut.SubmitAsync("https://example.com", ChannelKind.Web);

        await sut.MarkCompletedAsync(capture.Id, externalRef: "wal-42");

        var updated = await sut.GetByIdAsync(capture.Id);
        updated!.Stage.Should().Be(LifecycleStage.Completed);
        updated.ExternalRef.Should().Be("wal-42");
    }

    [Fact]
    public async Task MarkOrphanAsync_PersistsFailureReason()
    {
        var (_, sut, _) = Build();
        var capture = await sut.SubmitAsync("nonsense", ChannelKind.Web);

        await sut.MarkOrphanAsync(capture.Id, "no skill matched");

        var updated = await sut.GetByIdAsync(capture.Id);
        updated!.Stage.Should().Be(LifecycleStage.Orphan);
        updated.FailureReason.Should().Be("no skill matched");
    }

    [Fact]
    public async Task MarkUnhandledAsync_PersistsFailureReason()
    {
        var (_, sut, _) = Build();
        var capture = await sut.SubmitAsync("https://example.com", ChannelKind.Web);
        await sut.MarkClassifiedAsync(capture.Id, "Wallabag", "title");

        await sut.MarkUnhandledAsync(capture.Id, "wallabag returned 503");

        var updated = await sut.GetByIdAsync(capture.Id);
        updated!.Stage.Should().Be(LifecycleStage.Unhandled);
        updated.FailureReason.Should().Be("wallabag returned 503");
    }

    [Fact]
    public async Task ResetForRetryAsync_ResetsStageAndClearsFailureReason()
    {
        var (_, sut, _) = Build();
        var capture = await sut.SubmitAsync("https://example.com", ChannelKind.Web);
        await sut.MarkOrphanAsync(capture.Id, "test reason");

        await sut.ResetForRetryAsync(capture.Id);

        var updated = await sut.GetByIdAsync(capture.Id);
        updated!.Stage.Should().Be(LifecycleStage.Raw);
        updated.FailureReason.Should().BeNull();
    }

    [Fact]
    public async Task ListAsync_FiltersByStageAndSource()
    {
        var (_, sut, _) = Build();
        var orphan = await sut.SubmitAsync("orphan content", ChannelKind.Api);
        await sut.MarkOrphanAsync(orphan.Id, "test");
        await sut.SubmitAsync("https://example.com", ChannelKind.Web); // stays Raw

        var page = await sut.ListAsync(new CaptureFilter(
            Stages: new[] { LifecycleStage.Orphan },
            Source: ChannelKind.Api,
            Limit: 50,
            Cursor: null));

        page.Items.Should().HaveCount(1);
        page.Items[0].Id.Should().Be(orphan.Id);
    }

    [Fact]
    public async Task GetFailureCountsAsync_CountsOrphanAndUnhandled()
    {
        var (_, sut, _) = Build();
        var a = await sut.SubmitAsync("orphan-a", ChannelKind.Web);
        await sut.MarkOrphanAsync(a.Id, "x");
        var b = await sut.SubmitAsync("orphan-b", ChannelKind.Web);
        await sut.MarkOrphanAsync(b.Id, "x");
        var c = await sut.SubmitAsync("unhandled-a", ChannelKind.Web);
        await sut.MarkClassifiedAsync(c.Id, "Wallabag", "title");
        await sut.MarkUnhandledAsync(c.Id, "y");

        var counts = await sut.GetFailureCountsAsync();

        counts.OrphanCount.Should().Be(2);
        counts.UnhandledCount.Should().Be(1);
    }
}

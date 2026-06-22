using FlowHub.Core.Captures;
using FlowHub.Persistence.Repositories;
using FlowHub.Persistence.Tests.Fixtures;

namespace FlowHub.Persistence.Tests.Repositories;

[Collection(PostgresGroup.Name)]
public sealed class EfTagRepositoryTests(PostgresFixture fixture)
{
    private static async Task<Guid> SeedCaptureAsync(FlowHubDbContext db)
    {
        // Tags have a FK to Captures, so the parent row must exist first.
        var captures = new EfCaptureRepository(db);
        var cap = new Capture(Guid.NewGuid(), ChannelKind.Web, "x", DateTimeOffset.UtcNow, LifecycleStage.Raw, null);
        await captures.AddAsync(cap);
        return cap.Id;
    }

    [Fact]
    public async Task AddAsync_PersistsTag_AndGetByCaptureIdReturnsIt()
    {
        var db = await fixture.CreateFreshDbAsync(seedCatalog: false);
        var sut = new EfTagRepository(db);
        var captureId = await SeedCaptureAsync(db);

        await sut.AddAsync(captureId, "billing");

        var result = await sut.GetByCaptureIdAsync(captureId);
        result.Should().ContainSingle().Which.Should().Be("billing");
    }

    [Fact]
    public async Task AddAsync_MultipleTags_AllReturnedForSameCaptureId()
    {
        var db = await fixture.CreateFreshDbAsync(seedCatalog: false);
        var sut = new EfTagRepository(db);
        var captureId = await SeedCaptureAsync(db);

        await sut.AddAsync(captureId, "billing");
        await sut.AddAsync(captureId, "urgent");
        await sut.AddAsync(captureId, "client-x");

        var result = await sut.GetByCaptureIdAsync(captureId);
        result.Should().BeEquivalentTo(new[] { "billing", "urgent", "client-x" });
    }

    [Fact]
    public async Task GetByCaptureIdAsync_OnlyReturnsTagsForRequestedCapture()
    {
        var db = await fixture.CreateFreshDbAsync(seedCatalog: false);
        var sut = new EfTagRepository(db);
        var captureA = await SeedCaptureAsync(db);
        var captureB = await SeedCaptureAsync(db);
        await sut.AddAsync(captureA, "billing");
        await sut.AddAsync(captureB, "support");

        var result = await sut.GetByCaptureIdAsync(captureA);

        result.Should().ContainSingle().Which.Should().Be("billing");
    }

    [Fact]
    public async Task GetByCaptureIdAsync_NoTags_ReturnsEmpty()
    {
        var db = await fixture.CreateFreshDbAsync(seedCatalog: false);
        var sut = new EfTagRepository(db);

        var result = await sut.GetByCaptureIdAsync(Guid.NewGuid());

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task RemoveAsync_RemovesMatchingTag_LeavesOthers()
    {
        var db = await fixture.CreateFreshDbAsync(seedCatalog: false);
        var sut = new EfTagRepository(db);
        var captureId = await SeedCaptureAsync(db);
        await sut.AddAsync(captureId, "billing");
        await sut.AddAsync(captureId, "urgent");

        await sut.RemoveAsync(captureId, "billing");

        var result = await sut.GetByCaptureIdAsync(captureId);
        result.Should().ContainSingle().Which.Should().Be("urgent");
    }

    [Fact]
    public async Task RemoveAsync_NonExistentTag_DoesNotThrow_AndDoesNotChangeOthers()
    {
        var db = await fixture.CreateFreshDbAsync(seedCatalog: false);
        var sut = new EfTagRepository(db);
        var captureId = await SeedCaptureAsync(db);
        await sut.AddAsync(captureId, "keep");

        var act = () => sut.RemoveAsync(captureId, "never-there");

        await act.Should().NotThrowAsync();
        (await sut.GetByCaptureIdAsync(captureId)).Should().ContainSingle().Which.Should().Be("keep");
    }

    [Fact]
    public async Task RemoveAsync_OnlyTouchesRequestedCaptureId()
    {
        var db = await fixture.CreateFreshDbAsync(seedCatalog: false);
        var sut = new EfTagRepository(db);
        var captureA = await SeedCaptureAsync(db);
        var captureB = await SeedCaptureAsync(db);
        await sut.AddAsync(captureA, "shared");
        await sut.AddAsync(captureB, "shared");

        await sut.RemoveAsync(captureA, "shared");

        (await sut.GetByCaptureIdAsync(captureA)).Should().BeEmpty();
        (await sut.GetByCaptureIdAsync(captureB)).Should().ContainSingle().Which.Should().Be("shared");
    }
}

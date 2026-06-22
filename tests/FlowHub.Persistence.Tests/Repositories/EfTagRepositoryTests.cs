using FlowHub.Core.Captures;
using FlowHub.Persistence.Repositories;
using FlowHub.Persistence.Tests.Fixtures;

namespace FlowHub.Persistence.Tests.Repositories;

[Collection(PostgresGroup.Name)]
public sealed class EfTagRepositoryTests(PostgresFixture fixture)
{
    // TagEntity has a cascade FK to Capture(CaptureId), so every Tag needs a
    // parent Capture row before insert.
    private static async Task<Guid> SeedCapture(FlowHubDbContext db, string content = "tagged capture")
    {
        var captureRepo = new EfCaptureRepository(db);
        var capture = new Capture(
            Guid.NewGuid(), ChannelKind.Web, content, DateTimeOffset.UtcNow, LifecycleStage.Raw, null);
        await captureRepo.AddAsync(capture);
        return capture.Id;
    }

    [Fact]
    public async Task GetByCaptureIdAsync_NoTags_ReturnsEmpty()
    {
        var db = await fixture.CreateFreshDbAsync(seedCatalog: false);
        var repo = new EfTagRepository(db);

        var result = await repo.GetByCaptureIdAsync(Guid.NewGuid());

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task AddAsync_PersistsTag_AndGetByCaptureIdReturnsIt()
    {
        var db = await fixture.CreateFreshDbAsync(seedCatalog: false);
        var repo = new EfTagRepository(db);
        var captureId = await SeedCapture(db);

        await repo.AddAsync(captureId, "stoic");

        var result = await repo.GetByCaptureIdAsync(captureId);
        result.Should().ContainSingle().Which.Should().Be("stoic");
    }

    [Fact]
    public async Task GetByCaptureIdAsync_ScopesToOwnerCapture_OnlyOwnerTagsReturned()
    {
        var db = await fixture.CreateFreshDbAsync(seedCatalog: false);
        var repo = new EfTagRepository(db);
        var capA = await SeedCapture(db, "A");
        var capB = await SeedCapture(db, "B");
        await repo.AddAsync(capA, "alpha");
        await repo.AddAsync(capA, "beta");
        await repo.AddAsync(capB, "gamma");

        var result = await repo.GetByCaptureIdAsync(capA);

        result.Should().BeEquivalentTo(["alpha", "beta"]);
    }

    [Fact]
    public async Task RemoveAsync_KnownTag_DeletesIt()
    {
        var db = await fixture.CreateFreshDbAsync(seedCatalog: false);
        var repo = new EfTagRepository(db);
        var captureId = await SeedCapture(db);
        await repo.AddAsync(captureId, "alpha");
        await repo.AddAsync(captureId, "beta");

        await repo.RemoveAsync(captureId, "alpha");

        var result = await repo.GetByCaptureIdAsync(captureId);
        result.Should().BeEquivalentTo(["beta"]);
    }

    [Fact]
    public async Task RemoveAsync_UnknownTag_NoOps_DoesNotThrow()
    {
        var db = await fixture.CreateFreshDbAsync(seedCatalog: false);
        var repo = new EfTagRepository(db);
        var captureId = await SeedCapture(db);
        await repo.AddAsync(captureId, "alpha");

        var act = () => repo.RemoveAsync(captureId, "ghost");

        await act.Should().NotThrowAsync();
        var result = await repo.GetByCaptureIdAsync(captureId);
        result.Should().BeEquivalentTo(["alpha"]);
    }
}

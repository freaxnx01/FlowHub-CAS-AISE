using FlowHub.Core.Captures;
using FlowHub.Persistence.Repositories;
using FlowHub.Persistence.Tests.Fixtures;

namespace FlowHub.Persistence.Tests.Repositories;

[Collection(PostgresGroup.Name)]
public sealed class EfSkillRunRepositoryTests(PostgresFixture fixture)
{
    // SkillRuns have an FK to Captures; the parent row must exist first.
    private static async Task<Guid> SeedCaptureAsync(FlowHubDbContext db)
    {
        var captures = new EfCaptureRepository(db);
        var cap = new Capture(Guid.NewGuid(), ChannelKind.Web, "x", DateTimeOffset.UtcNow, LifecycleStage.Raw, null);
        await captures.AddAsync(cap);
        return cap.Id;
    }

    private static SkillRun BuildRun(
        Guid captureId,
        string skillName = "Books",
        DateTimeOffset? startedAt = null,
        bool success = true,
        string? failureReason = null)
    {
        var started = startedAt ?? DateTimeOffset.UtcNow;
        return new SkillRun(
            Guid.NewGuid(),
            skillName,
            captureId,
            started,
            started.AddSeconds(1),
            success,
            failureReason);
    }

    [Fact]
    public async Task AddAsync_PersistsSkillRun_AndReturnsInput()
    {
        var db = await fixture.CreateFreshDbAsync(seedCatalog: true);
        var sut = new EfSkillRunRepository(db);
        var captureId = await SeedCaptureAsync(db);
        var run = BuildRun(captureId);

        var saved = await sut.AddAsync(run);

        saved.Should().Be(run);

        var found = await sut.GetByCaptureIdAsync(captureId);
        found.Should().ContainSingle(r => r.Id == run.Id);
    }

    [Fact]
    public async Task AddAsync_PreservesAllFields()
    {
        var db = await fixture.CreateFreshDbAsync(seedCatalog: true);
        var sut = new EfSkillRunRepository(db);
        var captureId = await SeedCaptureAsync(db);
        var started = new DateTimeOffset(2026, 6, 22, 17, 30, 0, TimeSpan.Zero);
        var run = BuildRun(
            captureId: captureId,
            skillName: "Articles",
            startedAt: started,
            success: false,
            failureReason: "404 from API");

        await sut.AddAsync(run);

        var found = (await sut.GetByCaptureIdAsync(captureId)).Single();
        found.SkillName.Should().Be("Articles");
        found.Success.Should().BeFalse();
        found.FailureReason.Should().Be("404 from API");
        found.StartedAt.Should().Be(started);
        found.CompletedAt.Should().Be(started.AddSeconds(1));
    }

    [Fact]
    public async Task GetByCaptureIdAsync_OnlyReturnsRunsForRequestedCapture()
    {
        var db = await fixture.CreateFreshDbAsync(seedCatalog: true);
        var sut = new EfSkillRunRepository(db);
        var captureA = await SeedCaptureAsync(db);
        var captureB = await SeedCaptureAsync(db);
        await sut.AddAsync(BuildRun(captureA, skillName: "Books"));
        await sut.AddAsync(BuildRun(captureB, skillName: "Articles"));

        var result = await sut.GetByCaptureIdAsync(captureA);

        result.Should().ContainSingle().Which.SkillName.Should().Be("Books");
    }

    [Fact]
    public async Task GetByCaptureIdAsync_OrdersByStartedAtDescending()
    {
        var db = await fixture.CreateFreshDbAsync(seedCatalog: true);
        var sut = new EfSkillRunRepository(db);
        var captureId = await SeedCaptureAsync(db);
        var t0 = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);
        await sut.AddAsync(BuildRun(captureId, startedAt: t0));
        await sut.AddAsync(BuildRun(captureId, startedAt: t0.AddHours(2)));
        await sut.AddAsync(BuildRun(captureId, startedAt: t0.AddHours(1)));

        var result = await sut.GetByCaptureIdAsync(captureId);

        result.Select(r => r.StartedAt).Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task GetBySkillNameAsync_OnlyReturnsRunsForRequestedSkill()
    {
        var db = await fixture.CreateFreshDbAsync(seedCatalog: true);
        var sut = new EfSkillRunRepository(db);
        var captureId = await SeedCaptureAsync(db);
        await sut.AddAsync(BuildRun(captureId, skillName: "Books"));
        await sut.AddAsync(BuildRun(captureId, skillName: "Books"));
        await sut.AddAsync(BuildRun(captureId, skillName: "Articles"));

        var result = await sut.GetBySkillNameAsync("Books");

        result.Should().HaveCount(2);
        result.Should().OnlyContain(r => r.SkillName == "Books");
    }

    [Fact]
    public async Task GetBySkillNameAsync_OrdersByStartedAtDescending()
    {
        var db = await fixture.CreateFreshDbAsync(seedCatalog: true);
        var sut = new EfSkillRunRepository(db);
        var captureId = await SeedCaptureAsync(db);
        var t0 = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);
        await sut.AddAsync(BuildRun(captureId, skillName: "Books", startedAt: t0));
        await sut.AddAsync(BuildRun(captureId, skillName: "Books", startedAt: t0.AddDays(2)));
        await sut.AddAsync(BuildRun(captureId, skillName: "Books", startedAt: t0.AddDays(1)));

        var result = await sut.GetBySkillNameAsync("Books");

        result.Select(r => r.StartedAt).Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task GetByCaptureIdAsync_NoMatches_ReturnsEmpty()
    {
        var db = await fixture.CreateFreshDbAsync(seedCatalog: true);
        var sut = new EfSkillRunRepository(db);

        var result = await sut.GetByCaptureIdAsync(Guid.NewGuid());

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetBySkillNameAsync_NoMatches_ReturnsEmpty()
    {
        var db = await fixture.CreateFreshDbAsync(seedCatalog: true);
        var sut = new EfSkillRunRepository(db);

        var result = await sut.GetBySkillNameAsync("NotASkill");

        result.Should().BeEmpty();
    }
}

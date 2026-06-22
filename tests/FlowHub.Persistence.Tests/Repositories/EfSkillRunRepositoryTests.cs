using FlowHub.Core.Captures;
using FlowHub.Core.Health;
using FlowHub.Persistence.Repositories;
using FlowHub.Persistence.Tests.Fixtures;

namespace FlowHub.Persistence.Tests.Repositories;

[Collection(PostgresGroup.Name)]
public sealed class EfSkillRunRepositoryTests(PostgresFixture fixture)
{
    // SkillRunEntity has FKs to Skill (by SkillName, Restrict) and Capture (Cascade),
    // so each test seeds the parent rows via the existing EF repositories.
    private static async Task<(Guid captureId, string skillName)> SeedParents(
        FlowHubDbContext db, string skillName = "Movies", string content = "test capture")
    {
        var skillRepo = new EfSkillRepository(db);
        await skillRepo.UpsertAsync(new SkillHealth(skillName, HealthStatus.Healthy, 0));

        var captureRepo = new EfCaptureRepository(db);
        var capture = new Capture(
            Guid.NewGuid(), ChannelKind.Web, content, DateTimeOffset.UtcNow, LifecycleStage.Raw, null);
        await captureRepo.AddAsync(capture);

        return (capture.Id, skillName);
    }

    private static SkillRun NewRun(
        Guid captureId,
        string skillName,
        DateTimeOffset startedAt,
        bool success = true,
        string? failureReason = null) =>
        new(
            Guid.NewGuid(),
            skillName,
            captureId,
            startedAt,
            CompletedAt: startedAt.AddMilliseconds(150),
            Success: success,
            FailureReason: failureReason);

    [Fact]
    public async Task AddAsync_PersistsAllFields_AndReturnsInputUnchanged()
    {
        var db = await fixture.CreateFreshDbAsync(seedCatalog: false);
        var repo = new EfSkillRunRepository(db);
        var (captureId, skillName) = await SeedParents(db);
        var run = NewRun(captureId, skillName, DateTimeOffset.UtcNow);

        var returned = await repo.AddAsync(run);

        returned.Should().BeSameAs(run);

        var read = (await repo.GetByCaptureIdAsync(captureId)).Single();
        read.Id.Should().Be(run.Id);
        read.SkillName.Should().Be(skillName);
        read.CaptureId.Should().Be(captureId);
        read.StartedAt.Should().BeCloseTo(run.StartedAt, TimeSpan.FromMilliseconds(1));
        read.CompletedAt.Should().BeCloseTo(run.CompletedAt!.Value, TimeSpan.FromMilliseconds(1));
        read.Success.Should().BeTrue();
        read.FailureReason.Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_FailureRun_PersistsFailureReason_AndNullCompletedAt()
    {
        var db = await fixture.CreateFreshDbAsync(seedCatalog: false);
        var repo = new EfSkillRunRepository(db);
        var (captureId, skillName) = await SeedParents(db);

        var run = new SkillRun(
            Guid.NewGuid(), skillName, captureId,
            StartedAt: DateTimeOffset.UtcNow,
            CompletedAt: null,
            Success: false,
            FailureReason: "Wallabag 503");
        await repo.AddAsync(run);

        var read = (await repo.GetByCaptureIdAsync(captureId)).Single();
        read.Success.Should().BeFalse();
        read.CompletedAt.Should().BeNull();
        read.FailureReason.Should().Be("Wallabag 503");
    }

    [Fact]
    public async Task GetByCaptureIdAsync_ReturnsOnlyRowsForThatCapture_OrderedByStartedAtDesc()
    {
        var db = await fixture.CreateFreshDbAsync(seedCatalog: false);
        var repo = new EfSkillRunRepository(db);
        var (capA, skill) = await SeedParents(db, skillName: "Movies", content: "A");
        var (capB, _) = await SeedParents(db, skillName: "Movies", content: "B");

        var now = DateTimeOffset.UtcNow;
        await repo.AddAsync(NewRun(capA, skill, now.AddMinutes(-10)));
        await repo.AddAsync(NewRun(capA, skill, now.AddMinutes(-1)));
        await repo.AddAsync(NewRun(capB, skill, now)); // belongs to capture B, must be excluded

        var result = await repo.GetByCaptureIdAsync(capA);

        result.Should().HaveCount(2);
        result[0].StartedAt.Should().BeAfter(result[1].StartedAt);
    }

    [Fact]
    public async Task GetByCaptureIdAsync_NoRuns_ReturnsEmpty()
    {
        var db = await fixture.CreateFreshDbAsync(seedCatalog: false);
        var repo = new EfSkillRunRepository(db);

        var result = await repo.GetByCaptureIdAsync(Guid.NewGuid());

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetBySkillNameAsync_ReturnsOnlyRowsForThatSkill_OrderedByStartedAtDesc()
    {
        var db = await fixture.CreateFreshDbAsync(seedCatalog: false);
        var repo = new EfSkillRunRepository(db);
        var (capA, _) = await SeedParents(db, skillName: "Movies", content: "A");
        var (_, _) = await SeedParents(db, skillName: "Books", content: "B");

        var now = DateTimeOffset.UtcNow;
        await repo.AddAsync(NewRun(capA, "Movies", now.AddMinutes(-5)));
        await repo.AddAsync(NewRun(capA, "Movies", now));
        await repo.AddAsync(NewRun(capA, "Books",  now.AddMinutes(-2)));

        var result = await repo.GetBySkillNameAsync("Movies");

        result.Should().HaveCount(2);
        result.Should().OnlyContain(r => r.SkillName == "Movies");
        result[0].StartedAt.Should().BeAfter(result[1].StartedAt);
    }

    [Fact]
    public async Task GetBySkillNameAsync_UnknownSkill_ReturnsEmpty()
    {
        var db = await fixture.CreateFreshDbAsync(seedCatalog: false);
        var repo = new EfSkillRunRepository(db);

        var result = await repo.GetBySkillNameAsync("Nope");

        result.Should().BeEmpty();
    }
}

using FlowHub.Core.Health;
using FlowHub.Persistence.Repositories;
using FlowHub.Persistence.Tests.Fixtures;

namespace FlowHub.Persistence.Tests.Repositories;

[Collection(PostgresGroup.Name)]
public sealed class EfSkillRegistryTests(PostgresFixture fixture)
{
    [Fact]
    public async Task GetHealthAsync_ReturnsAllSkills()
    {
        var db = await fixture.CreateFreshDbAsync(seedCatalog: false);
        var skillRepo = new EfSkillRepository(db);
        await skillRepo.UpsertAsync(new SkillHealth("Wallabag", HealthStatus.Healthy, 5));
        await skillRepo.UpsertAsync(new SkillHealth("Vikunja", HealthStatus.Degraded, 2));
        var sut = new EfSkillRegistry(skillRepo);

        var result = await sut.GetHealthAsync();

        result.Should().HaveCount(2);
        result.Should().ContainSingle(s => s.Name == "Wallabag" && s.Status == HealthStatus.Healthy && s.RoutedToday == 5);
        result.Should().ContainSingle(s => s.Name == "Vikunja" && s.Status == HealthStatus.Degraded && s.RoutedToday == 2);
    }

    [Fact]
    public async Task GetHealthAsync_EmptyDb_ReturnsEmptyList()
    {
        var db = await fixture.CreateFreshDbAsync(seedCatalog: false);
        var sut = new EfSkillRegistry(new EfSkillRepository(db));

        var result = await sut.GetHealthAsync();

        result.Should().BeEmpty();
    }
}

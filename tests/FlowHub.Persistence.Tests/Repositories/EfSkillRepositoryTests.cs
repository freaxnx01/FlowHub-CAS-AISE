using FlowHub.Core.Health;
using FlowHub.Persistence.Repositories;
using FlowHub.Persistence.Tests.Fixtures;

namespace FlowHub.Persistence.Tests.Repositories;

[Collection(PostgresGroup.Name)]
public sealed class EfSkillRepositoryTests(PostgresFixture fixture)
{
    [Fact]
    public async Task UpsertAsync_NewName_InsertsRow()
    {
        var db = await fixture.CreateFreshDbAsync(seedCatalog: false);
        var sut = new EfSkillRepository(db);

        await sut.UpsertAsync(new SkillHealth("Books", HealthStatus.Healthy, 0));

        (await sut.GetByNameAsync("Books"))!.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task UpsertAsync_ExistingName_UpdatesStatusAndCount()
    {
        var db = await fixture.CreateFreshDbAsync(seedCatalog: false);
        var sut = new EfSkillRepository(db);
        await sut.UpsertAsync(new SkillHealth("Books", HealthStatus.Healthy, 0));

        await sut.UpsertAsync(new SkillHealth("Books", HealthStatus.Degraded, 7));

        var found = await sut.GetByNameAsync("Books");
        found!.Status.Should().Be(HealthStatus.Degraded);
        found.RoutedToday.Should().Be(7);
        (await sut.GetAllAsync()).Should().ContainSingle();
    }

    [Fact]
    public async Task GetByNameAsync_UnknownName_ReturnsNull()
    {
        var db = await fixture.CreateFreshDbAsync(seedCatalog: false);
        var sut = new EfSkillRepository(db);

        (await sut.GetByNameAsync("Nope")).Should().BeNull();
    }

    [Fact]
    public async Task IncrementRoutedTodayAsync_IncrementsCount_AndPersists()
    {
        var db = await fixture.CreateFreshDbAsync(seedCatalog: false);
        var sut = new EfSkillRepository(db);
        await sut.UpsertAsync(new SkillHealth("Books", HealthStatus.Healthy, 4));

        await sut.IncrementRoutedTodayAsync("Books");

        var found = await sut.GetByNameAsync("Books");
        found!.RoutedToday.Should().Be(5);
    }

    [Fact]
    public async Task IncrementRoutedTodayAsync_UnknownName_ThrowsKeyNotFound()
    {
        var db = await fixture.CreateFreshDbAsync(seedCatalog: false);
        var sut = new EfSkillRepository(db);

        var act = () => sut.IncrementRoutedTodayAsync("DoesNotExist");

        var ex = await act.Should().ThrowAsync<KeyNotFoundException>();
        ex.Which.Message.Should().Contain("DoesNotExist");
    }

    [Fact]
    public async Task IncrementRoutedTodayAsync_TwoIncrements_AddUp()
    {
        var db = await fixture.CreateFreshDbAsync(seedCatalog: false);
        var sut = new EfSkillRepository(db);
        await sut.UpsertAsync(new SkillHealth("Books", HealthStatus.Healthy, 0));

        await sut.IncrementRoutedTodayAsync("Books");
        await sut.IncrementRoutedTodayAsync("Books");

        (await sut.GetByNameAsync("Books"))!.RoutedToday.Should().Be(2);
    }

    [Fact]
    public async Task GetAllAsync_EmptyDb_ReturnsEmpty()
    {
        var db = await fixture.CreateFreshDbAsync(seedCatalog: false);
        var sut = new EfSkillRepository(db);

        (await sut.GetAllAsync()).Should().BeEmpty();
    }
}

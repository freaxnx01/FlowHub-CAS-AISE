using FlowHub.Core.Health;
using FlowHub.Persistence.Repositories;
using FlowHub.Persistence.Tests.Fixtures;

namespace FlowHub.Persistence.Tests.Repositories;

[Collection(PostgresGroup.Name)]
public sealed class EfIntegrationHealthServiceTests(PostgresFixture fixture)
{
    [Fact]
    public async Task GetHealthAsync_ReturnsAllIntegrations()
    {
        var db = await fixture.CreateFreshDbAsync(seedCatalog: false);
        var repo = new EfIntegrationRepository(db);
        await repo.UpsertAsync(new IntegrationHealth("Wallabag", HealthStatus.Healthy, DateTimeOffset.UtcNow.AddMinutes(-1), TimeSpan.FromMilliseconds(180)));
        var sut = new EfIntegrationHealthService(repo);

        var result = await sut.GetHealthAsync();

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Wallabag");
        result[0].Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task GetHealthAsync_MapsDurationMs_ToTimeSpan()
    {
        var db = await fixture.CreateFreshDbAsync(seedCatalog: false);
        var repo = new EfIntegrationRepository(db);
        await repo.UpsertAsync(new IntegrationHealth("Vikunja", HealthStatus.Healthy, null, TimeSpan.FromMilliseconds(250)));
        var sut = new EfIntegrationHealthService(repo);

        var result = await sut.GetHealthAsync();

        result[0].LastWriteDuration.Should().Be(TimeSpan.FromMilliseconds(250));
    }

    [Fact]
    public async Task GetHealthAsync_EmptyDb_ReturnsEmptyList()
    {
        var db = await fixture.CreateFreshDbAsync(seedCatalog: false);
        var sut = new EfIntegrationHealthService(new EfIntegrationRepository(db));

        var result = await sut.GetHealthAsync();

        result.Should().BeEmpty();
    }
}

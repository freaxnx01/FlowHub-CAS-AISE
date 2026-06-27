using FlowHub.Core.Health;
using FlowHub.Persistence.Repositories;
using FlowHub.Persistence.Tests.Fixtures;

namespace FlowHub.Persistence.Tests.Repositories;

[Collection(PostgresGroup.Name)]
public sealed class EfIntegrationRepositoryTests(PostgresFixture fixture)
{
    [Fact]
    public async Task UpsertAsync_NewName_InsertsRow()
    {
        var db = await fixture.CreateFreshDbAsync(seedCatalog: false);
        var sut = new EfIntegrationRepository(db);

        await sut.UpsertAsync(new IntegrationHealth("Wallabag", HealthStatus.Healthy, null, null));

        var found = await sut.GetByNameAsync("Wallabag");
        found.Should().NotBeNull();
        found!.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task UpsertAsync_ExistingName_UpdatesStatusAndTimingFields()
    {
        var db = await fixture.CreateFreshDbAsync(seedCatalog: false);
        var sut = new EfIntegrationRepository(db);
        await sut.UpsertAsync(new IntegrationHealth("Vikunja", HealthStatus.Healthy, null, null));
        var when = new DateTimeOffset(2026, 6, 22, 12, 0, 0, TimeSpan.Zero);

        await sut.UpsertAsync(new IntegrationHealth("Vikunja", HealthStatus.Degraded, when, TimeSpan.FromMilliseconds(420)));

        var found = await sut.GetByNameAsync("Vikunja");
        found.Should().NotBeNull();
        found!.Status.Should().Be(HealthStatus.Degraded);
        found.LastWriteAt.Should().Be(when);
        found.LastWriteDuration.Should().Be(TimeSpan.FromMilliseconds(420));

        // Still only one row.
        (await sut.GetAllAsync()).Should().ContainSingle();
    }

    [Fact]
    public async Task UpsertAsync_NullDuration_RoundTripsAsNull()
    {
        var db = await fixture.CreateFreshDbAsync(seedCatalog: false);
        var sut = new EfIntegrationRepository(db);

        await sut.UpsertAsync(new IntegrationHealth("Paperless", HealthStatus.Healthy, null, LastWriteDuration: null));

        var found = await sut.GetByNameAsync("Paperless");
        found!.LastWriteDuration.Should().BeNull();
    }

    [Fact]
    public async Task GetByNameAsync_UnknownName_ReturnsNull()
    {
        var db = await fixture.CreateFreshDbAsync(seedCatalog: false);
        var sut = new EfIntegrationRepository(db);

        (await sut.GetByNameAsync("Nope")).Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEveryUpsertedIntegration()
    {
        var db = await fixture.CreateFreshDbAsync(seedCatalog: false);
        var sut = new EfIntegrationRepository(db);
        await sut.UpsertAsync(new IntegrationHealth("Wallabag", HealthStatus.Healthy, null, null));
        await sut.UpsertAsync(new IntegrationHealth("Vikunja", HealthStatus.Degraded, null, null));
        await sut.UpsertAsync(new IntegrationHealth("Paperless", HealthStatus.Down, null, null));

        var all = await sut.GetAllAsync();

        all.Select(i => i.Name).Should().BeEquivalentTo(new[] { "Wallabag", "Vikunja", "Paperless" });
    }

    [Fact]
    public async Task AddHealthSampleAsync_PersistsSample_AndGetRecentReturnsIt()
    {
        var db = await fixture.CreateFreshDbAsync(seedCatalog: false);
        var sut = new EfIntegrationRepository(db);
        await sut.UpsertAsync(new IntegrationHealth("Wallabag", HealthStatus.Healthy, null, null));

        await sut.AddHealthSampleAsync("Wallabag", HealthStatus.Healthy, TimeSpan.FromMilliseconds(150));

        var samples = await sut.GetRecentSamplesAsync("Wallabag", count: 10);
        samples.Should().ContainSingle();
        samples[0].IntegrationName.Should().Be("Wallabag");
        samples[0].Status.Should().Be(HealthStatus.Healthy);
        samples[0].Duration.Should().Be(TimeSpan.FromMilliseconds(150));
    }

    [Fact]
    public async Task AddHealthSampleAsync_NullDuration_RoundTripsAsNull()
    {
        var db = await fixture.CreateFreshDbAsync(seedCatalog: false);
        var sut = new EfIntegrationRepository(db);
        await sut.UpsertAsync(new IntegrationHealth("Vikunja", HealthStatus.Down, null, null));

        await sut.AddHealthSampleAsync("Vikunja", HealthStatus.Down, duration: null);

        var samples = await sut.GetRecentSamplesAsync("Vikunja", count: 10);
        samples.Should().ContainSingle();
        samples[0].Duration.Should().BeNull();
    }

    [Fact]
    public async Task GetRecentSamplesAsync_OnlyReturnsSamplesForRequestedIntegration()
    {
        var db = await fixture.CreateFreshDbAsync(seedCatalog: false);
        var sut = new EfIntegrationRepository(db);
        await sut.UpsertAsync(new IntegrationHealth("Wallabag", HealthStatus.Healthy, null, null));
        await sut.UpsertAsync(new IntegrationHealth("Vikunja", HealthStatus.Healthy, null, null));
        await sut.AddHealthSampleAsync("Wallabag", HealthStatus.Healthy, TimeSpan.FromMilliseconds(100));
        await sut.AddHealthSampleAsync("Vikunja", HealthStatus.Healthy, TimeSpan.FromMilliseconds(200));

        var samples = await sut.GetRecentSamplesAsync("Wallabag", count: 10);

        samples.Should().ContainSingle();
        samples[0].IntegrationName.Should().Be("Wallabag");
    }

    [Fact]
    public async Task GetRecentSamplesAsync_OrdersDescendingBySampledAt_AndRespectsCount()
    {
        var db = await fixture.CreateFreshDbAsync(seedCatalog: false);
        var sut = new EfIntegrationRepository(db);
        await sut.UpsertAsync(new IntegrationHealth("Wallabag", HealthStatus.Healthy, null, null));
        // Three samples in arbitrary order; the repo sorts SampledAt DESC and takes `count`.
        for (var i = 0; i < 3; i++)
        {
            await sut.AddHealthSampleAsync("Wallabag", HealthStatus.Healthy, TimeSpan.FromMilliseconds(i + 1));
            await Task.Delay(2); // ensure SampledAt is strictly monotonic
        }

        var samples = await sut.GetRecentSamplesAsync("Wallabag", count: 2);

        samples.Should().HaveCount(2);
        samples.Select(s => s.SampledAt).Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task GetRecentSamplesAsync_NoMatches_ReturnsEmpty()
    {
        var db = await fixture.CreateFreshDbAsync(seedCatalog: false);
        var sut = new EfIntegrationRepository(db);

        var samples = await sut.GetRecentSamplesAsync("NeverUsed", count: 10);

        samples.Should().BeEmpty();
    }
}

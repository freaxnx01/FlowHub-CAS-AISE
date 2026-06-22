using FlowHub.Core.Captures;
using FlowHub.Core.Channels;
using FlowHub.Core.Health;
using FlowHub.Persistence.Repositories;
using FlowHub.Persistence.Tests.Fixtures;

namespace FlowHub.Persistence.Tests.Repositories;

[Collection(PostgresGroup.Name)]
public sealed class EfChannelRepositoryTests(PostgresFixture fixture)
{
    private static Channel NewChannel(
        string name,
        ChannelKind kind = ChannelKind.Web,
        bool enabled = true,
        HealthStatus status = HealthStatus.Healthy,
        DateTimeOffset? lastActive = null) =>
        new(name, kind, enabled, status, lastActive);

    [Fact]
    public async Task GetAllAsync_EmptyDb_ReturnsEmpty()
    {
        var db = await fixture.CreateFreshDbAsync(seedCatalog: false);
        var repo = new EfChannelRepository(db);

        var result = await repo.GetAllAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_WithSeededChannels_ReturnsAllAsDomain()
    {
        var db = await fixture.CreateFreshDbAsync(seedCatalog: false);
        var repo = new EfChannelRepository(db);
        await repo.UpsertAsync(NewChannel("Web", ChannelKind.Web));
        await repo.UpsertAsync(NewChannel("Telegram", ChannelKind.Telegram, enabled: false, status: HealthStatus.Down));

        var result = await repo.GetAllAsync();

        result.Should().HaveCount(2);
        result.Should().ContainSingle(c =>
            c.Name == "Web" && c.Kind == ChannelKind.Web && c.IsEnabled && c.Status == HealthStatus.Healthy);
        result.Should().ContainSingle(c =>
            c.Name == "Telegram" && c.Kind == ChannelKind.Telegram && !c.IsEnabled && c.Status == HealthStatus.Down);
    }

    [Fact]
    public async Task GetByNameAsync_KnownName_ReturnsChannel()
    {
        var db = await fixture.CreateFreshDbAsync(seedCatalog: false);
        var repo = new EfChannelRepository(db);
        var lastActive = DateTimeOffset.UtcNow.AddMinutes(-3);
        await repo.UpsertAsync(NewChannel("Api", ChannelKind.Api, lastActive: lastActive));

        var result = await repo.GetByNameAsync("Api");

        result.Should().NotBeNull();
        result!.Name.Should().Be("Api");
        result.Kind.Should().Be(ChannelKind.Api);
        result.LastActiveAt.Should().BeCloseTo(lastActive, TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task GetByNameAsync_UnknownName_ReturnsNull()
    {
        var db = await fixture.CreateFreshDbAsync(seedCatalog: false);
        var repo = new EfChannelRepository(db);

        var result = await repo.GetByNameAsync("does-not-exist");

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpsertAsync_NewChannel_InsertsRow()
    {
        var db = await fixture.CreateFreshDbAsync(seedCatalog: false);
        var repo = new EfChannelRepository(db);

        await repo.UpsertAsync(NewChannel("Telegram", ChannelKind.Telegram, status: HealthStatus.Degraded));

        var stored = await repo.GetByNameAsync("Telegram");
        stored.Should().NotBeNull();
        stored!.Kind.Should().Be(ChannelKind.Telegram);
        stored.Status.Should().Be(HealthStatus.Degraded);
    }

    [Fact]
    public async Task UpsertAsync_ExistingChannel_UpdatesMutableFields()
    {
        var db = await fixture.CreateFreshDbAsync(seedCatalog: false);
        var repo = new EfChannelRepository(db);
        await repo.UpsertAsync(NewChannel("Web", ChannelKind.Web, enabled: true, status: HealthStatus.Healthy));

        var newActiveAt = DateTimeOffset.UtcNow;
        await repo.UpsertAsync(new Channel("Web", ChannelKind.Api, IsEnabled: false, HealthStatus.Down, newActiveAt));

        var stored = await repo.GetByNameAsync("Web");
        stored.Should().NotBeNull();
        stored!.Kind.Should().Be(ChannelKind.Api);
        stored.IsEnabled.Should().BeFalse();
        stored.Status.Should().Be(HealthStatus.Down);
        stored.LastActiveAt.Should().BeCloseTo(newActiveAt, TimeSpan.FromMilliseconds(1));

        // Should be an update, not a duplicate insert.
        (await repo.GetAllAsync()).Should().ContainSingle();
    }
}

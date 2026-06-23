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
        string name = "Web",
        ChannelKind kind = ChannelKind.Web,
        bool isEnabled = true,
        HealthStatus status = HealthStatus.Healthy,
        DateTimeOffset? lastActiveAt = null) =>
        new(name, kind, isEnabled, status, lastActiveAt);

    [Fact]
    public async Task UpsertAsync_NewName_InsertsRow()
    {
        var db = await fixture.CreateFreshDbAsync(seedCatalog: false);
        var sut = new EfChannelRepository(db);

        await sut.UpsertAsync(NewChannel("Web", ChannelKind.Web, true, HealthStatus.Healthy));

        var all = await sut.GetAllAsync();
        all.Should().ContainSingle(c => c.Name == "Web");
    }

    [Fact]
    public async Task UpsertAsync_ExistingName_UpdatesAllFields()
    {
        var db = await fixture.CreateFreshDbAsync(seedCatalog: false);
        var sut = new EfChannelRepository(db);
        await sut.UpsertAsync(NewChannel("Web", ChannelKind.Web, true, HealthStatus.Healthy, null));
        var when = new DateTimeOffset(2026, 6, 22, 12, 0, 0, TimeSpan.Zero);

        await sut.UpsertAsync(NewChannel("Web", ChannelKind.Api, isEnabled: false, status: HealthStatus.Degraded, lastActiveAt: when));

        var fetched = await sut.GetByNameAsync("Web");
        fetched.Should().NotBeNull();
        fetched!.Kind.Should().Be(ChannelKind.Api);
        fetched.IsEnabled.Should().BeFalse();
        fetched.Status.Should().Be(HealthStatus.Degraded);
        fetched.LastActiveAt.Should().Be(when);

        // And there's still only one row.
        (await sut.GetAllAsync()).Should().ContainSingle();
    }

    [Fact]
    public async Task GetByNameAsync_UnknownName_ReturnsNull()
    {
        var db = await fixture.CreateFreshDbAsync(seedCatalog: false);
        var sut = new EfChannelRepository(db);

        (await sut.GetByNameAsync("Nope")).Should().BeNull();
    }

    [Fact]
    public async Task GetByNameAsync_KnownName_RoundTripsAllFields()
    {
        var db = await fixture.CreateFreshDbAsync(seedCatalog: false);
        var sut = new EfChannelRepository(db);
        var when = new DateTimeOffset(2026, 6, 22, 12, 0, 0, TimeSpan.Zero);
        await sut.UpsertAsync(NewChannel("Telegram", ChannelKind.Telegram, isEnabled: false, status: HealthStatus.Down, lastActiveAt: when));

        var found = await sut.GetByNameAsync("Telegram");

        found.Should().NotBeNull();
        found!.Name.Should().Be("Telegram");
        found.Kind.Should().Be(ChannelKind.Telegram);
        found.IsEnabled.Should().BeFalse();
        found.Status.Should().Be(HealthStatus.Down);
        found.LastActiveAt.Should().Be(when);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEveryUpsertedChannel()
    {
        var db = await fixture.CreateFreshDbAsync(seedCatalog: false);
        var sut = new EfChannelRepository(db);
        await sut.UpsertAsync(NewChannel("Web"));
        await sut.UpsertAsync(NewChannel("Api", ChannelKind.Api));
        await sut.UpsertAsync(NewChannel("Telegram", ChannelKind.Telegram));

        var all = await sut.GetAllAsync();

        all.Select(c => c.Name).Should().BeEquivalentTo(new[] { "Web", "Api", "Telegram" });
    }

    [Fact]
    public async Task GetAllAsync_EmptyDb_ReturnsEmpty()
    {
        var db = await fixture.CreateFreshDbAsync(seedCatalog: false);
        var sut = new EfChannelRepository(db);

        (await sut.GetAllAsync()).Should().BeEmpty();
    }
}

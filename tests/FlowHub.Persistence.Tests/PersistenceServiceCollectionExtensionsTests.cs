using FlowHub.Core.Captures;
using FlowHub.Core.Channels;
using FlowHub.Core.Health;
using FlowHub.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlowHub.Persistence.Tests;

public sealed class PersistenceServiceCollectionExtensionsTests
{
    // Stub IConfiguration that backs only the indexer + GetSection — enough for
    // `GetConnectionString("Default")` which boils down to `cfg["ConnectionStrings:Default"]`.
    // Avoids pulling Microsoft.Extensions.Configuration{,Memory} into this test
    // project just for an empty-config builder.
    private static IConfiguration StubConfig(string? connectionString = null)
    {
        var cfg = Substitute.For<IConfiguration>();
        cfg["ConnectionStrings:Default"].Returns(connectionString);
        return cfg;
    }

    private static ServiceCollection Build(string? connectionString = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFlowHubPersistence(StubConfig(connectionString));
        return services;
    }

    private static bool IsRegisteredAsScoped<TService, TImpl>(IServiceCollection services) =>
        services.Any(d =>
            d.ServiceType == typeof(TService) &&
            d.ImplementationType == typeof(TImpl) &&
            d.Lifetime == ServiceLifetime.Scoped);

    [Fact]
    public void AddFlowHubPersistence_RegistersEveryScopedRepoAndService()
    {
        var services = Build();

        IsRegisteredAsScoped<ICaptureRepository,        EfCaptureRepository>(services).Should().BeTrue();
        IsRegisteredAsScoped<ICaptureService,           EfCaptureService>(services).Should().BeTrue();
        IsRegisteredAsScoped<IChannelRepository,        EfChannelRepository>(services).Should().BeTrue();
        IsRegisteredAsScoped<ISkillRepository,          EfSkillRepository>(services).Should().BeTrue();
        IsRegisteredAsScoped<ISkillRegistry,            EfSkillRegistry>(services).Should().BeTrue();
        IsRegisteredAsScoped<IIntegrationRepository,    EfIntegrationRepository>(services).Should().BeTrue();
        IsRegisteredAsScoped<IIntegrationHealthService, EfIntegrationHealthService>(services).Should().BeTrue();
        IsRegisteredAsScoped<ITagRepository,            EfTagRepository>(services).Should().BeTrue();
        IsRegisteredAsScoped<ISkillRunRepository,       EfSkillRunRepository>(services).Should().BeTrue();
    }

    [Fact]
    public void AddFlowHubPersistence_RegistersFlowHubDbContext_ScopedFromAddDbContext()
    {
        var services = Build();

        services.Should().Contain(d =>
            d.ServiceType == typeof(FlowHubDbContext) && d.Lifetime == ServiceLifetime.Scoped);
        services.Should().Contain(d =>
            d.ServiceType == typeof(DbContextOptions<FlowHubDbContext>));
    }

    // The `configuration.GetConnectionString("Default") ?? DefaultConnectionString`
    // branch is covered just by invoking the extension with each shape of config —
    // verifying the resulting Npgsql connection string requires reflecting into
    // EF Core internals that change between provider versions. We assert the call
    // succeeds + the DbContext can be resolved; the connection string itself is
    // exercised at runtime by the Testcontainers integration suite.

    [Fact]
    public void AddFlowHubPersistence_NoConfiguredConnectionString_ResolvesDbContext()
    {
        var services = Build();

        using var sp = services.BuildServiceProvider();
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FlowHubDbContext>();
        db.Should().NotBeNull();
    }

    [Fact]
    public void AddFlowHubPersistence_WithConfiguredConnectionString_ResolvesDbContext()
    {
        var services = Build("Host=db.example.com;Port=5433;Database=other;Username=u;Password=p");

        using var sp = services.BuildServiceProvider();
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FlowHubDbContext>();
        db.Should().NotBeNull();
    }

    [Fact]
    public void AddFlowHubPersistence_DefaultsIEmbeddingServiceToNullEmbeddingService()
    {
        var services = Build();

        using var sp = services.BuildServiceProvider();
        sp.GetRequiredService<IEmbeddingService>().Should().BeOfType<NullEmbeddingService>();
    }

    [Fact]
    public void AddFlowHubPersistence_RespectsPreRegisteredIEmbeddingService()
    {
        // TryAddSingleton must NOT replace a service that was already registered
        // (e.g. by FlowHub.AI when an embedding provider is configured).
        var services = new ServiceCollection();
        services.AddLogging();
        var preExisting = Substitute.For<IEmbeddingService>();
        services.AddSingleton(preExisting);

        services.AddFlowHubPersistence(StubConfig());

        using var sp = services.BuildServiceProvider();
        sp.GetRequiredService<IEmbeddingService>().Should().BeSameAs(preExisting);
    }

    [Fact]
    public async Task NullEmbeddingService_GenerateAsync_ReturnsNull()
    {
        var result = await NullEmbeddingService.Instance.GenerateAsync("anything");

        result.Should().BeNull();
    }
}

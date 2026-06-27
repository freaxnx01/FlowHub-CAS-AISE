using FlowHub.Core.Captures;
using FlowHub.Core.Channels;
using FlowHub.Core.Health;
using FlowHub.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FlowHub.Persistence;

public static class PersistenceServiceCollectionExtensions
{
    private const string DefaultConnectionString =
        "Host=localhost;Port=5432;Database=flowhub;Username=flowhub;Password=dev-secret";

    public static IServiceCollection AddFlowHubPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default") ?? DefaultConnectionString;

        services.AddDbContext<FlowHubDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql => npgsql.UseVector()));
        services.AddScoped<ICaptureRepository, EfCaptureRepository>();
        services.AddScoped<ICaptureService, EfCaptureService>();
        services.AddScoped<IChannelRepository, EfChannelRepository>();
        services.AddScoped<ISkillRepository, EfSkillRepository>();
        services.AddScoped<ISkillRegistry, EfSkillRegistry>();
        services.AddScoped<IIntegrationRepository, EfIntegrationRepository>();
        services.AddScoped<IIntegrationHealthService, EfIntegrationHealthService>();
        services.AddScoped<ITagRepository, EfTagRepository>();
        services.AddScoped<ISkillRunRepository, EfSkillRunRepository>();
        services.TryAddSingleton<IEmbeddingService>(NullEmbeddingService.Instance);

        return services;
    }
}

/// <summary>Returned when no embedding provider is configured.</summary>
internal sealed class NullEmbeddingService : IEmbeddingService
{
    public static readonly NullEmbeddingService Instance = new();
    public Task<float[]?> GenerateAsync(string text, CancellationToken cancellationToken = default)
        => Task.FromResult<float[]?>(null);
}

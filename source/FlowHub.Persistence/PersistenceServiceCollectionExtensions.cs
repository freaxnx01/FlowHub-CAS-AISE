using FlowHub.Core.Captures;
using Pgvector.EntityFrameworkCore;
using FlowHub.Core.Channels;
using FlowHub.Core.Health;
using FlowHub.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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

/// <remarks>
/// Not registered as IHostedService in production (12-Factor XII).
/// Use the <c>flowhub.migrations</c> Docker Compose service or <c>make migrate</c>.
/// </remarks>
internal sealed partial class MigrationRunner : IHostedService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<MigrationRunner> _log;

    public MigrationRunner(IServiceProvider services, ILogger<MigrationRunner> log)
    {
        _services = services;
        _log = log;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FlowHubDbContext>();
        LogApplyingMigrations();
        await db.Database.MigrateAsync(cancellationToken);
        LogMigrationsApplied();
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(EventId = 5010, Level = LogLevel.Information, Message = "Applying EF Core migrations…")]
    private partial void LogApplyingMigrations();

    [LoggerMessage(EventId = 5011, Level = LogLevel.Information, Message = "EF Core migrations up-to-date.")]
    private partial void LogMigrationsApplied();
}

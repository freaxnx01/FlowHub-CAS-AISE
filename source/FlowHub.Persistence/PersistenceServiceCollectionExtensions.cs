using FlowHub.Core.Captures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FlowHub.Persistence;

public static class PersistenceServiceCollectionExtensions
{
    private const string DefaultConnectionString = "Data Source=flowhub.db";

    public static IServiceCollection AddFlowHubPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default") ?? DefaultConnectionString;

        services.AddDbContext<FlowHubDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<ICaptureService, EfCaptureService>();
        services.AddHostedService<MigrationRunner>();

        return services;
    }
}

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

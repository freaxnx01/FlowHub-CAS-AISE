using FlowHub.Persistence;
using FlowHub.Persistence.Entities;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace FlowHub.Api.IntegrationTests;

/// <summary>
/// Boots FlowHub.Web in-process with the Development environment so the
/// DevAuthHandler bypass is active (no real OIDC token required).
/// Replaces the SQLite DbContext registration with EF Core InMemory so each
/// test factory instance gets an isolated, file-free database.
/// Seeds a fixed set of captures so tests that expect pre-existing data
/// (Orphan, Completed, and enough rows for cursor pagination) work without stubs.
/// </summary>
public sealed class IntegrationTestFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = Guid.NewGuid().ToString("N");

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            // Strip all DbContext registrations that AddFlowHubPersistence wired up.
            // In EF Core 8+, AddDbContext registers three service types per context:
            //   • DbContextOptions<TContext>                 — the resolved options object
            //   • IDbContextOptionsConfiguration<TContext>  — the builder action (carries the provider)
            //   • DbContextOptions                          — the non-generic fallback
            // We must remove the IDbContextOptionsConfiguration<FlowHubDbContext> entry too;
            // leaving it causes EF to see two providers (Sqlite + InMemory) and throw.
            services.RemoveAll<DbContextOptions<FlowHubDbContext>>();

            // IDbContextOptionsConfiguration<T> is internal to EF — access it by interface name.
            var efDbContextOptsConfigType = services
                .Select(d => d.ServiceType)
                .FirstOrDefault(t =>
                    t.IsGenericType &&
                    t.GetGenericTypeDefinition().Name == "IDbContextOptionsConfiguration`1" &&
                    t.GenericTypeArguments.Length == 1 &&
                    t.GenericTypeArguments[0] == typeof(FlowHubDbContext));

            if (efDbContextOptsConfigType is not null)
            {
                services.RemoveAll(efDbContextOptsConfigType);
            }

            // Surgically remove only the MigrationRunner hosted service.
            // Wholesale RemoveAll<IHostedService>() would also drop MassTransit's bus-control
            // hosted service, causing consumer pipeline tests to fail.
            var runners = services
                .Where(d => d.ImplementationType?.Name == "MigrationRunner")
                .ToList();
            foreach (var d in runners)
            {
                services.Remove(d);
            }

            services.AddDbContext<FlowHubDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));
        });

        var host = base.CreateHost(builder);
        SeedDatabase(host.Services);
        return host;
    }

    /// <summary>
    /// Seeds a deterministic set of captures so tests that rely on pre-existing data pass:
    /// - 10 rows total (enough for two cursor pages at limit=5 and limit=3)
    /// - 2 Orphan rows (RetryCapture tests query stage=Orphan&amp;limit=1)
    /// - 2 Completed rows (RetryCapture tests query stage=Completed&amp;limit=1)
    /// - Remaining rows are Raw / Classified to cover stage-filter assertions
    /// </summary>
    private static void SeedDatabase(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FlowHubDbContext>();

        var now = DateTimeOffset.UtcNow;
        var entities = new List<CaptureEntity>
        {
            MakeCapture("https://example.com/a1", "Web",   "Raw",       now.AddSeconds(-10)),
            MakeCapture("https://example.com/a2", "Web",   "Raw",       now.AddSeconds(-20)),
            MakeCapture("https://example.com/a3", "Api",   "Classified",now.AddSeconds(-30)),
            MakeCapture("https://example.com/a4", "Api",   "Classified",now.AddSeconds(-40)),
            MakeCapture("https://example.com/a5", "Web",   "Routed",    now.AddSeconds(-50)),
            MakeCapture("https://example.com/a6", "Web",   "Completed", now.AddSeconds(-60)),
            MakeCapture("https://example.com/a7", "Api",   "Completed", now.AddSeconds(-70)),
            MakeCapture("https://example.com/a8", "Web",   "Orphan",    now.AddSeconds(-80)),
            MakeCapture("https://example.com/a9", "Web",   "Orphan",    now.AddSeconds(-90)),
            MakeCapture("https://example.com/a10","Api",   "Unhandled", now.AddSeconds(-100)),
        };

        db.Captures.AddRange(entities);
        db.SaveChanges();
    }

    private static CaptureEntity MakeCapture(string content, string source, string stage, DateTimeOffset createdAt) =>
        new()
        {
            Id = Guid.NewGuid(),
            Content = content,
            Source = source,
            Stage = stage,
            CreatedAt = createdAt,
        };
}

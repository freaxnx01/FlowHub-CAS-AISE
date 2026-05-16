using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace FlowHub.Persistence.Tests.Fixtures;

public sealed class PostgresFixture : IAsyncLifetime
{
    private PostgreSqlContainer _container = null!;
    public string ConnectionString { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("pgvector/pgvector:pg17")
            .WithDatabase("flowhub_test")
            .WithUsername("test")
            .WithPassword("test")
            .Build();
        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    public async Task<FlowHubDbContext> CreateFreshDbAsync(bool seedCatalog = true)
    {
        var dbName = $"testdb_{Guid.NewGuid():N}";

        await using var adminConn = new NpgsqlConnection(ConnectionString);
        await adminConn.OpenAsync();
        await using var cmd = adminConn.CreateCommand();
        cmd.CommandText = $"CREATE DATABASE \"{dbName}\"";
        await cmd.ExecuteNonQueryAsync();

        var csb = new NpgsqlConnectionStringBuilder(ConnectionString) { Database = dbName, PersistSecurityInfo = true };
        var options = new DbContextOptionsBuilder<FlowHubDbContext>()
            .UseNpgsql(csb.ConnectionString, npgsql => npgsql.UseVector())
            .Options;
        var db = new FlowHubDbContext(options);
        await db.Database.MigrateAsync();

        if (!seedCatalog)
        {
            await db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"Skills\", \"Integrations\" RESTART IDENTITY CASCADE");
        }

        return db;
    }
}

[CollectionDefinition(Name)]
public sealed class PostgresGroup : ICollectionFixture<PostgresFixture>
{
    public const string Name = "Postgres";
}

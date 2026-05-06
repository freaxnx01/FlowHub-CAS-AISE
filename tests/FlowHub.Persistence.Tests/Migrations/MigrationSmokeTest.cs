using FlowHub.Persistence.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FlowHub.Persistence.Tests.Migrations;

[Collection(PostgresGroup.Name)]
public sealed class MigrationSmokeTest(PostgresFixture fixture)
{
    private static readonly string[] ExpectedTables =
    [
        "Captures", "Channels", "Skills", "SkillRuns",
        "Integrations", "IntegrationHealthSamples", "Tags"
    ];

    [Fact]
    public async Task ApplyAllMigrations_AllExpectedTablesExist()
    {
        var db = await fixture.CreateFreshDbAsync();

        foreach (var table in ExpectedTables)
        {
            await using var conn = new NpgsqlConnection(db.Database.GetConnectionString());
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText =
                "SELECT COUNT(*) FROM information_schema.tables " +
                "WHERE table_schema='public' AND table_name=@name";
            cmd.Parameters.AddWithValue("name", table);
            var result = (long)(await cmd.ExecuteScalarAsync())!;
            result.Should().Be(1, $"table '{table}' should exist after migration");
        }
    }

    [Fact]
    public async Task ApplyMigrations_Idempotent_WhenCalledTwice()
    {
        var db = await fixture.CreateFreshDbAsync();

        var act = async () => await db.Database.MigrateAsync();

        await act.Should().NotThrowAsync("second MigrateAsync call should be a no-op");
    }
}

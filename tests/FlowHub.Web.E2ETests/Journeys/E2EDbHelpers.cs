using Npgsql;

namespace FlowHub.Web.E2ETests.Journeys;

/// <summary>
/// Test-side direct DB writes used to engineer states the live API can't
/// reach against an empty/stub-integration system. The connection string
/// matches the docker-compose Postgres + the dev FlowHubDbContextFactory
/// fallback. Override via FLOWHUB_E2E_DB.
/// </summary>
internal static class E2EDbHelpers
{
    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("FLOWHUB_E2E_DB")
        ?? "Host=localhost;Port=5432;Database=flowhub;Username=flowhub;Password=dev-secret";

    /// <summary>
    /// Idempotent: insert (or update to) a Completed-stage capture with a stable Id.
    /// Used by J15, which needs a Completed capture without depending on a real
    /// integration roundtrip.
    /// </summary>
    public static async Task UpsertCompletedCaptureAsync(Guid id, string content, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync(ct);
        await using var cmd = new NpgsqlCommand(@"
            INSERT INTO ""Captures"" (""Id"", ""Source"", ""Content"", ""CreatedAt"", ""Stage"", ""MatchedSkill"", ""ExternalRef"")
            VALUES (@id, 'Web', @content, @created, 'Completed', 'Books', 'http://test.example/' || @id::text)
            ON CONFLICT (""Id"") DO UPDATE
                SET ""Stage"" = 'Completed',
                    ""MatchedSkill"" = 'Books',
                    ""FailureReason"" = NULL,
                    ""ExternalRef"" = 'http://test.example/' || @id::text", conn);
        cmd.Parameters.AddWithValue("id", id);
        cmd.Parameters.AddWithValue("content", content);
        cmd.Parameters.AddWithValue("created", DateTimeOffset.UtcNow);
        await cmd.ExecuteNonQueryAsync(ct);
    }
}

using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FlowHub.Persistence;

// Invoked only by the EF Core CLI/tooling pipeline (e.g. `dotnet ef migrations add`,
// the `dotnet ef migrations bundle` step in docker/migrations/Dockerfile). Never
// reached at runtime, so it's excluded from coverage rather than wrapped in a test
// that would just re-implement the same `UseNpgsql` call.
[ExcludeFromCodeCoverage]
internal sealed class FlowHubDbContextFactory : IDesignTimeDbContextFactory<FlowHubDbContext>
{
    public FlowHubDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? "Host=localhost;Port=5432;Database=flowhub;Username=flowhub;Password=dev-secret";

        var options = new DbContextOptionsBuilder<FlowHubDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.UseVector())
            .Options;

        return new FlowHubDbContext(options);
    }
}

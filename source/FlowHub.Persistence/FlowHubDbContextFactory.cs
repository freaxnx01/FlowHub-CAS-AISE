using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Pgvector.EntityFrameworkCore;

namespace FlowHub.Persistence;

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

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FlowHub.Persistence;

/// <summary>
/// Design-time factory so that <c>dotnet ef migrations add</c> can instantiate
/// <see cref="FlowHubDbContext"/> without a running application host.
/// Used only by the EF Core CLI tooling — never registered in DI.
/// </summary>
internal sealed class FlowHubDbContextFactory : IDesignTimeDbContextFactory<FlowHubDbContext>
{
    public FlowHubDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<FlowHubDbContext>()
            .UseSqlite("Data Source=flowhub.db")
            .Options;

        return new FlowHubDbContext(options);
    }
}

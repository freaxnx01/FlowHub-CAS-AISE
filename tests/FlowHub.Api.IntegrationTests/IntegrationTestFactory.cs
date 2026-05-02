using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;

namespace FlowHub.Api.IntegrationTests;

/// <summary>
/// Boots FlowHub.Web in-process with the Development environment so the
/// DevAuthHandler bypass is active (no real OIDC token required).
/// </summary>
public sealed class IntegrationTestFactory : WebApplicationFactory<Program>
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        return base.CreateHost(builder);
    }
}

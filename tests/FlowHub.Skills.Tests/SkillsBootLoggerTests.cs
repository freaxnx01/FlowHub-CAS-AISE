using FlowHub.Core.Skills;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FlowHub.Skills.Tests;

public sealed class SkillsBootLoggerTests
{
    [Fact]
    public async Task StartAsync_WithMixedOutcomes_LogsRegisteredAndNotConfiguredThenStops()
    {
        // Wallabag fully configured (Registered=true) while Vikunja/Paperless stay
        // unconfigured (Registered=false) → exercises both branches of the boot logger.
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Skills:Wallabag:BaseUrl"] = "https://wallabag.example.com",
                ["Skills:Wallabag:ClientId"] = "client-id",
                ["Skills:Wallabag:ClientSecret"] = "client-secret",
                ["Skills:Wallabag:Username"] = "user",
                ["Skills:Wallabag:Password"] = "pass",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFlowHubSkills(configuration);
        await using var sp = services.BuildServiceProvider();

        var outcomes = sp.GetServices<SkillsRegistrationOutcome>().ToList();
        outcomes.Should().Contain(o => o.Registered);   // Wallabag
        outcomes.Should().Contain(o => !o.Registered);  // Vikunja / Paperless

        var hosted = sp.GetServices<IHostedService>().ToList();
        hosted.Should().NotBeEmpty();

        foreach (var service in hosted)
        {
            await service.StartAsync(CancellationToken.None);
            await service.StopAsync(CancellationToken.None);
        }
    }
}

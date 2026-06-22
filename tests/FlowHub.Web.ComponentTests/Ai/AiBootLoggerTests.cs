using FlowHub.AI;
using FlowHub.Core.Skills;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FlowHub.Web.ComponentTests.Ai;

public sealed class AiBootLoggerTests
{
    [Fact]
    public async Task StartAsync_WhenProviderConfigured_LogsRegisteredBranch_AndStops()
    {
        // Covers the "UsesAi=true" branch of AiBootLogger (provider + model line),
        // which the existing "no-provider" cases skip.
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Ai:Provider"] = "Anthropic",
                ["Ai:Anthropic:ApiKey"] = "sk-ant-test",
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        var catalog = Substitute.For<IVikunjaProjectCatalog>();
        catalog.GetAsync(Arg.Any<CancellationToken>())
               .Returns(new Dictionary<string, int> { ["Inbox"] = 1 });
        services.AddSingleton(catalog);
        services.AddFlowHubAi(config);
        await using var sp = services.BuildServiceProvider();

        sp.GetRequiredService<AiRegistrationOutcome>().UsesAi.Should().BeTrue();

        var hosted = sp.GetServices<IHostedService>().OfType<AiBootLogger>().ToList();
        hosted.Should().ContainSingle();

        foreach (var service in hosted)
        {
            await service.StartAsync(CancellationToken.None);
            await service.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task StartAsync_WhenProviderNotConfigured_LogsNotConfiguredBranch_AndStops()
    {
        // Sanity-cover the not-configured branch (Reason logged) end-to-end via DI,
        // independent of the AddFlowHubAi tests that only inspect the outcome.
        var config = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFlowHubAi(config);
        await using var sp = services.BuildServiceProvider();

        sp.GetRequiredService<AiRegistrationOutcome>().UsesAi.Should().BeFalse();

        var hosted = sp.GetServices<IHostedService>().OfType<AiBootLogger>().ToList();
        hosted.Should().ContainSingle();

        foreach (var service in hosted)
        {
            await service.StartAsync(CancellationToken.None);
            await service.StopAsync(CancellationToken.None);
        }
    }
}

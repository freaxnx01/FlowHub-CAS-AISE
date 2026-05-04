using FlowHub.Core.Skills;
using FlowHub.Skills;
using FlowHub.Skills.Wallabag;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FlowHub.Skills.Tests;

public sealed class SkillsServiceCollectionExtensionsTests
{
    private static ServiceProvider Build(Dictionary<string, string?> config)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(config).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFlowHubSkills(configuration);
        return services.BuildServiceProvider();
    }

    [Fact]
    public void AddFlowHubSkills_NoConfig_RegistersNoIntegrationsAndOneNotConfiguredOutcome()
    {
        var sp = Build(new Dictionary<string, string?>());

        sp.GetServices<ISkillIntegration>().Should().BeEmpty();
        var outcomes = sp.GetServices<SkillsRegistrationOutcome>().ToList();
        outcomes.Should().ContainSingle(o => o.Skill == "Wallabag" && !o.Registered);
    }

    [Fact]
    public void AddFlowHubSkills_WallabagFullyConfigured_RegistersIntegration()
    {
        var sp = Build(new Dictionary<string, string?>
        {
            ["Skills:Wallabag:BaseUrl"] = "https://wallabag.example.com",
            ["Skills:Wallabag:ApiToken"] = "tok",
        });

        var integrations = sp.GetServices<ISkillIntegration>().ToList();
        integrations.Should().ContainSingle(i => i.Name == "Wallabag");
        var outcome = sp.GetServices<SkillsRegistrationOutcome>().Single(o => o.Skill == "Wallabag");
        outcome.Registered.Should().BeTrue();
        outcome.Reason.Should().Be("configured");
    }

    [Fact]
    public void AddFlowHubSkills_WallabagBaseUrlOnly_DoesNotRegisterAndReportsMissingToken()
    {
        var sp = Build(new Dictionary<string, string?>
        {
            ["Skills:Wallabag:BaseUrl"] = "https://wallabag.example.com",
        });

        sp.GetServices<ISkillIntegration>().Should().BeEmpty();
        sp.GetServices<SkillsRegistrationOutcome>()
            .Single(o => o.Skill == "Wallabag")
            .Reason.Should().Be("missing-api-token");
    }

    [Fact]
    public void AddFlowHubSkills_WallabagTokenOnly_DoesNotRegisterAndReportsMissingBaseUrl()
    {
        var sp = Build(new Dictionary<string, string?>
        {
            ["Skills:Wallabag:ApiToken"] = "tok",
        });

        sp.GetServices<ISkillIntegration>().Should().BeEmpty();
        sp.GetServices<SkillsRegistrationOutcome>()
            .Single(o => o.Skill == "Wallabag")
            .Reason.Should().Be("missing-base-url");
    }
}

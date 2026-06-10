using FlowHub.Core.Captures;
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
            ["Skills:Wallabag:ClientId"] = "client-id",
            ["Skills:Wallabag:ClientSecret"] = "client-secret",
            ["Skills:Wallabag:Username"] = "user",
            ["Skills:Wallabag:Password"] = "pass",
        });

        var integrations = sp.GetServices<ISkillIntegration>().ToList();
        integrations.Should().ContainSingle(i => i.Name == "Wallabag");
        var outcome = sp.GetServices<SkillsRegistrationOutcome>().Single(o => o.Skill == "Wallabag");
        outcome.Registered.Should().BeTrue();
        outcome.Reason.Should().Be("configured");
    }

    [Fact]
    public void AddFlowHubSkills_WallabagBaseUrlWithoutClientId_DoesNotRegisterAndReportsMissingClientId()
    {
        var sp = Build(new Dictionary<string, string?>
        {
            ["Skills:Wallabag:BaseUrl"] = "https://wallabag.example.com",
        });

        sp.GetServices<ISkillIntegration>().Should().BeEmpty();
        sp.GetServices<SkillsRegistrationOutcome>()
            .Single(o => o.Skill == "Wallabag")
            .Reason.Should().Be("missing-client-id");
    }

    [Fact]
    public void AddFlowHubSkills_WallabagWithoutUsername_DoesNotRegisterAndReportsMissingUsername()
    {
        var sp = Build(new Dictionary<string, string?>
        {
            ["Skills:Wallabag:BaseUrl"] = "https://wallabag.example.com",
            ["Skills:Wallabag:ClientId"] = "client-id",
        });

        sp.GetServices<ISkillIntegration>().Should().BeEmpty();
        sp.GetServices<SkillsRegistrationOutcome>()
            .Single(o => o.Skill == "Wallabag")
            .Reason.Should().Be("missing-username");
    }

    [Fact]
    public void AddFlowHubSkills_WallabagWithoutBaseUrl_DoesNotRegisterAndReportsMissingBaseUrl()
    {
        var sp = Build(new Dictionary<string, string?>
        {
            ["Skills:Wallabag:ClientId"] = "client-id",
            ["Skills:Wallabag:Username"] = "user",
        });

        sp.GetServices<ISkillIntegration>().Should().BeEmpty();
        sp.GetServices<SkillsRegistrationOutcome>()
            .Single(o => o.Skill == "Wallabag")
            .Reason.Should().Be("missing-base-url");
    }

    [Fact]
    public void AddFlowHubSkills_VikunjaFullyConfigured_RegistersIntegration()
    {
        var sp = Build(new Dictionary<string, string?>
        {
            ["Skills:Vikunja:BaseUrl"] = "https://vikunja.example.com",
            ["Skills:Vikunja:ApiToken"] = "tok",
            ["Skills:Vikunja:FallbackProjectId"] = "42",
        });

        sp.GetServices<ISkillIntegration>().Should().ContainSingle(i => i.Name == "Vikunja");
        sp.GetServices<SkillsRegistrationOutcome>()
            .Single(o => o.Skill == "Vikunja")
            .Registered.Should().BeTrue();
    }

    [Fact]
    public void AddFlowHubSkills_VikunjaWithoutProjectId_NotRegistered()
    {
        var sp = Build(new Dictionary<string, string?>
        {
            ["Skills:Vikunja:BaseUrl"] = "https://vikunja.example.com",
            ["Skills:Vikunja:ApiToken"] = "tok",
        });

        sp.GetServices<ISkillIntegration>().Should().NotContain(i => i.Name == "Vikunja");
        sp.GetServices<SkillsRegistrationOutcome>()
            .Single(o => o.Skill == "Vikunja")
            .Reason.Should().Be("missing-fallback-project-id");
    }

    [Fact]
    public void AddFlowHubSkills_BothConfigured_RegistersTwoIntegrations()
    {
        var sp = Build(new Dictionary<string, string?>
        {
            ["Skills:Wallabag:BaseUrl"] = "https://wallabag.example.com",
            ["Skills:Wallabag:ClientId"] = "client-id",
            ["Skills:Wallabag:ClientSecret"] = "client-secret",
            ["Skills:Wallabag:Username"] = "user",
            ["Skills:Wallabag:Password"] = "pass",
            ["Skills:Vikunja:BaseUrl"] = "https://vikunja.example.com",
            ["Skills:Vikunja:ApiToken"] = "vik-tok",
            ["Skills:Vikunja:FallbackProjectId"] = "42",
        });

        var integrations = sp.GetServices<ISkillIntegration>().Select(i => i.Name).ToList();
        integrations.Should().BeEquivalentTo(new[] { "Wallabag", "Vikunja" });
    }

    [Fact]
    public void AddFlowHubSkills_WithPaperlessConfigured_RegistersPaperlessIntegration()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Skills:Paperless:BaseUrl"] = "https://paperless.example.com",
            ["Skills:Paperless:ApiToken"] = "tok",
        }).Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IAttachmentStorage>(Substitute.For<IAttachmentStorage>());
        services.AddFlowHubSkills(configuration);

        using var sp = services.BuildServiceProvider();
        sp.GetServices<ISkillIntegration>().Should().Contain(i => i.Name == "Paperless");
    }

    [Fact]
    public void AddFlowHubSkills_WithoutPaperlessConfig_DoesNotRegisterPaperless()
    {
        var sp = Build(new Dictionary<string, string?>());

        sp.GetServices<ISkillIntegration>().Should().NotContain(i => i.Name == "Paperless");
    }
}

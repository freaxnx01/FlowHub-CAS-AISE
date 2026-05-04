using FlowHub.Core.Skills;
using FlowHub.Skills.Vikunja;
using FlowHub.Skills.Wallabag;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlowHub.Skills;

public static class SkillsServiceCollectionExtensions
{
    public static IServiceCollection AddFlowHubSkills(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHostedService<SkillsBootLogger>();

        AddWallabag(services, configuration);
        AddVikunja(services, configuration);

        return services;
    }

    private static void AddWallabag(IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(WallabagOptions.SectionName);
        var options = section.Get<WallabagOptions>() ?? new WallabagOptions();

        if (string.IsNullOrWhiteSpace(options.BaseUrl) || string.IsNullOrWhiteSpace(options.ApiToken))
        {
            services.AddSingleton(new SkillsRegistrationOutcome("Wallabag", Registered: false,
                Reason: string.IsNullOrWhiteSpace(options.BaseUrl) ? "missing-base-url" : "missing-api-token"));
            return;
        }

        services.Configure<WallabagOptions>(section);
        services.AddHttpClient<WallabagSkillIntegration>(client =>
        {
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(10);
        });
        services.AddSingleton<ISkillIntegration>(sp => sp.GetRequiredService<WallabagSkillIntegration>());
        services.AddSingleton(new SkillsRegistrationOutcome("Wallabag", Registered: true, Reason: "configured"));
    }

    private static void AddVikunja(IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(VikunjaOptions.SectionName);
        var options = section.Get<VikunjaOptions>() ?? new VikunjaOptions();

        string? reason = null;
        if (string.IsNullOrWhiteSpace(options.BaseUrl)) { reason = "missing-base-url"; }
        else if (string.IsNullOrWhiteSpace(options.ApiToken)) { reason = "missing-api-token"; }
        else if (options.DefaultProjectId <= 0) { reason = "missing-project-id"; }

        if (reason is not null)
        {
            services.AddSingleton(new SkillsRegistrationOutcome("Vikunja", Registered: false, Reason: reason));
            return;
        }

        services.Configure<VikunjaOptions>(section);
        services.AddHttpClient<VikunjaSkillIntegration>(client =>
        {
            client.BaseAddress = new Uri(options.BaseUrl!);
            client.Timeout = TimeSpan.FromSeconds(10);
        });
        services.AddSingleton<ISkillIntegration>(sp => sp.GetRequiredService<VikunjaSkillIntegration>());
        services.AddSingleton(new SkillsRegistrationOutcome("Vikunja", Registered: true, Reason: "configured"));
    }
}

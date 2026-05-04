using FlowHub.Core.Skills;
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
}

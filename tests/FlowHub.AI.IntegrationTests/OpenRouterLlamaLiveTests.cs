using FlowHub.AI;
using FlowHub.Core.Classification;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FlowHub.AI.IntegrationTests;

[Trait("Category", "AI")]
public sealed class OpenRouterLlamaLiveTests
{
    private static IClassifier BuildClassifier()
    {
        var apiKey = Environment.GetEnvironmentVariable("Ai__OpenRouter__ApiKey");
        Skip.If(string.IsNullOrWhiteSpace(apiKey), "Ai__OpenRouter__ApiKey not configured");

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Ai:Provider"] = "OpenRouter",
                ["Ai:OpenRouter:ApiKey"] = apiKey,
            }).Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFlowHubAi(config);
        return services.BuildServiceProvider().GetRequiredService<IClassifier>();
    }

    [SkippableFact]
    public async Task ClassifyAsync_UrlContent_LiveOpenRouterReturnsWallabagWithTitle()
    {
        var sut = BuildClassifier();

        var result = await sut.ClassifyAsync(
            "https://en.wikipedia.org/wiki/Modular_monolith",
            CancellationToken.None);

        result.MatchedSkill.Should().Be("Wallabag");
        result.Title.Should().NotBeNullOrWhiteSpace();
    }

    [SkippableFact]
    public async Task ClassifyAsync_TodoContent_LiveOpenRouterReturnsVikunjaWithTitle()
    {
        var sut = BuildClassifier();

        var result = await sut.ClassifyAsync(
            "todo: review the Block 3 PVA submission tomorrow",
            CancellationToken.None);

        result.MatchedSkill.Should().Be("Vikunja");
        result.Title.Should().NotBeNullOrWhiteSpace();
    }
}

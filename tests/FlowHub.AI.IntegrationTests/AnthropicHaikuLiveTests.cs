using FlowHub.AI;
using FlowHub.Core.Classification;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FlowHub.AI.IntegrationTests;

[Trait("Category", "AI")]
public sealed class AnthropicHaikuLiveTests
{
    private static IClassifier BuildClassifier()
    {
        var apiKey = Environment.GetEnvironmentVariable("Ai__Anthropic__ApiKey");
        Skip.If(string.IsNullOrWhiteSpace(apiKey), "Ai__Anthropic__ApiKey not configured");

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Ai:Provider"] = "Anthropic",
                ["Ai:Anthropic:ApiKey"] = apiKey,
            }).Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFlowHubAi(config);
        return services.BuildServiceProvider().GetRequiredService<IClassifier>();
    }

    [SkippableFact]
    public async Task ClassifyAsync_UrlContent_LiveAnthropicReturnsWallabagWithTitle()
    {
        var sut = BuildClassifier();

        var result = await sut.ClassifyAsync(
            "https://en.wikipedia.org/wiki/Hexagonal_architecture",
            CancellationToken.None);

        result.MatchedSkill.Should().Be("Wallabag");
        result.Title.Should().NotBeNullOrWhiteSpace();
    }

    [SkippableFact]
    public async Task ClassifyAsync_TodoContent_LiveAnthropicReturnsVikunjaWithTitle()
    {
        var sut = BuildClassifier();

        var result = await sut.ClassifyAsync(
            "todo: buy milk on Saturday",
            CancellationToken.None);

        result.MatchedSkill.Should().Be("Vikunja");
        result.Title.Should().NotBeNullOrWhiteSpace();
    }
}

using FlowHub.Core.Captures;
using FlowHub.Skills.Wallabag;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FlowHub.Skills.IntegrationTests;

[Trait("Category", "BetaSmoke")]
public sealed class WallabagLiveTests
{
    [SkippableFact]
    public async Task HandleAsync_LiveWallabag_PostsUrlAndReturnsExternalRef()
    {
        var baseUrl = Environment.GetEnvironmentVariable("Skills__Wallabag__BaseUrl");
        var token = Environment.GetEnvironmentVariable("Skills__Wallabag__ApiToken");
        Skip.If(string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(token),
            "Skills__Wallabag__BaseUrl/ApiToken not configured");

        using var http = new HttpClient { BaseAddress = new Uri(baseUrl!), Timeout = TimeSpan.FromSeconds(15) };
        var sut = new WallabagSkillIntegration(
            http,
            Options.Create(new WallabagOptions { BaseUrl = baseUrl, ApiToken = token }),
            NullLogger<WallabagSkillIntegration>.Instance);

        var capture = new Capture(
            Id: Guid.NewGuid(),
            Source: ChannelKind.Web,
            Content: "https://en.wikipedia.org/wiki/Hexagonal_architecture",
            CreatedAt: DateTimeOffset.UtcNow,
            Stage: LifecycleStage.Classified,
            MatchedSkill: "Wallabag",
            Title: "Hexagonal architecture");

        var result = await sut.HandleAsync(capture, default);

        result.Success.Should().BeTrue();
        result.ExternalRef.Should().NotBeNullOrWhiteSpace();
    }
}

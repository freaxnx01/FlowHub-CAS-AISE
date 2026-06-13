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
        var clientId = Environment.GetEnvironmentVariable("Skills__Wallabag__ClientId");
        var clientSecret = Environment.GetEnvironmentVariable("Skills__Wallabag__ClientSecret");
        var username = Environment.GetEnvironmentVariable("Skills__Wallabag__Username");
        var password = Environment.GetEnvironmentVariable("Skills__Wallabag__Password");
        Skip.If(
            string.IsNullOrWhiteSpace(baseUrl)
            || string.IsNullOrWhiteSpace(clientId)
            || string.IsNullOrWhiteSpace(username),
            "Skills__Wallabag__BaseUrl/ClientId/Username not configured");

        var options = Options.Create(new WallabagOptions
        {
            BaseUrl = baseUrl,
            ClientId = clientId,
            ClientSecret = clientSecret,
            Username = username,
            Password = password,
        });

        using var tokenHttp = new HttpClient { BaseAddress = new Uri(baseUrl!), Timeout = TimeSpan.FromSeconds(15) };
        using var tokenProvider = new WallabagTokenProvider(
            tokenHttp,
            options,
            TimeProvider.System,
            NullLogger<WallabagTokenProvider>.Instance);

        using var http = new HttpClient { BaseAddress = new Uri(baseUrl!), Timeout = TimeSpan.FromSeconds(15) };
        var sut = new WallabagSkillIntegration(
            http,
            tokenProvider,
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

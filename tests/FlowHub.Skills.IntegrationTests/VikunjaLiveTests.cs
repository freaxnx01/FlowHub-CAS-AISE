using FlowHub.Core.Captures;
using FlowHub.Skills.Vikunja;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FlowHub.Skills.IntegrationTests;

[Trait("Category", "BetaSmoke")]
public sealed class VikunjaLiveTests
{
    [SkippableFact]
    public async Task HandleAsync_LiveVikunja_PutsTaskAndReturnsExternalRef()
    {
        var baseUrl = Environment.GetEnvironmentVariable("Skills__Vikunja__BaseUrl");
        var token = Environment.GetEnvironmentVariable("Skills__Vikunja__ApiToken");
        var projectIdRaw = Environment.GetEnvironmentVariable("Skills__Vikunja__DefaultProjectId");
        Skip.If(string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(projectIdRaw),
            "Skills__Vikunja__BaseUrl/ApiToken/DefaultProjectId not configured");

        var projectId = int.Parse(projectIdRaw!, System.Globalization.CultureInfo.InvariantCulture);

        using var http = new HttpClient { BaseAddress = new Uri(baseUrl!), Timeout = TimeSpan.FromSeconds(15) };
        var sut = new VikunjaSkillIntegration(
            http,
            Options.Create(new VikunjaOptions { BaseUrl = baseUrl, ApiToken = token, DefaultProjectId = projectId }),
            NullLogger<VikunjaSkillIntegration>.Instance);

        var capture = new Capture(
            Id: Guid.NewGuid(),
            Source: ChannelKind.Web,
            Content: $"todo: FlowHub Beta smoke test {DateTimeOffset.UtcNow:O}",
            CreatedAt: DateTimeOffset.UtcNow,
            Stage: LifecycleStage.Classified,
            MatchedSkill: "Vikunja",
            Title: $"FlowHub Beta smoke test {DateTimeOffset.UtcNow:O}");

        var result = await sut.HandleAsync(capture, default);

        result.Success.Should().BeTrue();
        result.ExternalRef.Should().NotBeNullOrWhiteSpace();
    }
}

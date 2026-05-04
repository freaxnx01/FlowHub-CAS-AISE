using System.Net;
using System.Net.Http;
using FlowHub.Core.Captures;
using FlowHub.Skills.Vikunja;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RichardSzalay.MockHttp;

namespace FlowHub.Skills.Tests.Vikunja;

public sealed class VikunjaSkillIntegrationTests
{
    private static (VikunjaSkillIntegration sut, MockHttpMessageHandler mock) Build(VikunjaOptions? options = null)
    {
        options ??= new VikunjaOptions
        {
            BaseUrl = "https://vikunja.example.com",
            ApiToken = "test-token",
            DefaultProjectId = 42,
        };
        var mock = new MockHttpMessageHandler();
        var http = mock.ToHttpClient();
        http.BaseAddress = new Uri(options.BaseUrl!);
        return (new VikunjaSkillIntegration(http, Options.Create(options), NullLogger<VikunjaSkillIntegration>.Instance), mock);
    }

    private static Capture TodoCapture(string content, string? title = "Buy milk on Saturday") => new(
        Id: Guid.NewGuid(),
        Source: ChannelKind.Web,
        Content: content,
        CreatedAt: DateTimeOffset.UtcNow,
        Stage: LifecycleStage.Classified,
        MatchedSkill: "Vikunja",
        Title: title);

    [Fact]
    public void Name_IsVikunja()
    {
        var (sut, _) = Build();
        sut.Name.Should().Be("Vikunja");
    }

    [Fact]
    public async Task HandleAsync_PutsTaskWithTitleToConfiguredProject_ReturnsExternalRef()
    {
        var (sut, mock) = Build();
        mock.Expect(HttpMethod.Put, "https://vikunja.example.com/api/v1/projects/42/tasks")
            .WithHeaders("Authorization", "Bearer test-token")
            .WithPartialContent("\"title\":\"Buy milk on Saturday\"")
            .Respond("application/json", """{"id":777}""");

        var result = await sut.HandleAsync(TodoCapture("todo: buy milk on Saturday"), default);

        result.Success.Should().BeTrue();
        result.ExternalRef.Should().Be("777");
        mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task HandleAsync_NoTitleOnCapture_FallsBackToTrimmedContent()
    {
        var (sut, mock) = Build();
        mock.Expect(HttpMethod.Put, "*/api/v1/projects/42/tasks")
            .WithPartialContent("\"title\":\"todo: buy milk\"")
            .Respond("application/json", """{"id":1}""");

        var result = await sut.HandleAsync(TodoCapture("todo: buy milk", title: null), default);

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_ServerReturns401_ThrowsHttpRequestException()
    {
        var (sut, mock) = Build();
        mock.Expect(HttpMethod.Put, "*/api/v1/projects/42/tasks").Respond(HttpStatusCode.Unauthorized);

        var act = () => sut.HandleAsync(TodoCapture("todo: x"), default);

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task HandleAsync_ServerReturns503_ThrowsHttpRequestException()
    {
        var (sut, mock) = Build();
        mock.Expect(HttpMethod.Put, "*/api/v1/projects/42/tasks").Respond(HttpStatusCode.ServiceUnavailable);

        var act = () => sut.HandleAsync(TodoCapture("todo: x"), default);

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task HandleAsync_ResponseWithoutId_ThrowsInvalidOperationException()
    {
        var (sut, mock) = Build();
        mock.Expect(HttpMethod.Put, "*/api/v1/projects/42/tasks").Respond("application/json", "{}");

        var act = () => sut.HandleAsync(TodoCapture("todo: x"), default);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*id*");
    }
}

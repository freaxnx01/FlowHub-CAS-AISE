using System.Net;
using FlowHub.Core.Captures;
using FlowHub.Core.Skills;
using FlowHub.Skills.Vikunja;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace FlowHub.Skills.ContractTests.Vikunja;

[Trait("Category", "SkillContract")]
public sealed class VikunjaContractTests : IClassFixture<WireMockServerFixture>, IDisposable
{
    private const int ProjectId = 42;
    private const string ApiToken = "test-token";

    private readonly WireMockServerFixture _wire;
    private readonly HttpClient _http;
    private readonly VikunjaSkillIntegration _sut;

    public VikunjaContractTests(WireMockServerFixture wire)
    {
        _wire = wire;
        _wire.Reset();

        var options = new VikunjaOptions
        {
            BaseUrl = _wire.BaseUrl,
            ApiToken = ApiToken,
            FallbackProjectId = ProjectId,
        };
        var catalog = Substitute.For<IVikunjaProjectCatalog>();
        catalog.GetAsync(Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, int> { [options.FallbackProject] = ProjectId });

        _http = new HttpClient { BaseAddress = new Uri(_wire.BaseUrl) };
        _sut = new VikunjaSkillIntegration(
            _http,
            Options.Create(options),
            catalog,
            NullLogger<VikunjaSkillIntegration>.Instance);
    }

    public void Dispose() => _http.Dispose();

    private static Capture TodoCapture(string content = "todo: buy milk", string? title = "Buy milk on Saturday") =>
        new(
            Id: Guid.NewGuid(),
            Source: ChannelKind.Web,
            Content: content,
            CreatedAt: DateTimeOffset.UtcNow,
            Stage: LifecycleStage.Classified,
            MatchedSkill: "Vikunja",
            Title: title);

    [Fact]
    public async Task HandleAsync_HappyPath_PutsTaskAndReturnsExternalRef()
    {
        _wire.Server
            .Given(Request.Create()
                .WithPath($"/api/v1/projects/{ProjectId}/tasks")
                .UsingPut()
                .WithHeader("Authorization", $"Bearer {ApiToken}")
                .WithBody(b => b is not null && b.Contains("\"title\":\"Buy milk on Saturday\"")))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"id":777}"""));

        var result = await _sut.HandleAsync(TodoCapture(), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.ExternalRef.Should().Be("777");
    }

    [Fact]
    public async Task HandleAsync_FallsBackToContent_WhenTitleMissing()
    {
        _wire.Server
            .Given(Request.Create()
                .WithPath($"/api/v1/projects/{ProjectId}/tasks")
                .UsingPut()
                .WithBody(b => b is not null && b.Contains("\"title\":\"todo: review the proposal\"")))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBodyAsJson(new { id = 99L }));

        var result = await _sut.HandleAsync(
            TodoCapture(content: "todo: review the proposal", title: null),
            CancellationToken.None);

        result.Success.Should().BeTrue();
        result.ExternalRef.Should().Be("99");
    }

    [Fact]
    public async Task HandleAsync_ServerReturns401_ThrowsHttpRequestException()
    {
        _wire.Server
            .Given(Request.Create().WithPath($"/api/v1/projects/{ProjectId}/tasks").UsingPut())
            .RespondWith(Response.Create().WithStatusCode(HttpStatusCode.Unauthorized));

        var act = () => _sut.HandleAsync(TodoCapture(), CancellationToken.None);

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task HandleAsync_ServerReturns500_ThrowsHttpRequestException()
    {
        _wire.Server
            .Given(Request.Create().WithPath($"/api/v1/projects/{ProjectId}/tasks").UsingPut())
            .RespondWith(Response.Create().WithStatusCode(HttpStatusCode.InternalServerError));

        var act = () => _sut.HandleAsync(TodoCapture(), CancellationToken.None);

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task HandleAsync_ResponseBodyMissingId_ThrowsInvalidOperation()
    {
        _wire.Server
            .Given(Request.Create().WithPath($"/api/v1/projects/{ProjectId}/tasks").UsingPut())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBody("""{"name":"no id here"}"""));

        var act = () => _sut.HandleAsync(TodoCapture(), CancellationToken.None);

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*did not include an 'id' field*");
    }

    [Fact]
    public async Task HandleAsync_SendsBearerToken_OnExactPath()
    {
        _wire.Server
            .Given(Request.Create()
                .WithPath($"/api/v1/projects/{ProjectId}/tasks")
                .UsingPut())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBodyAsJson(new { id = 1L }));

        await _sut.HandleAsync(TodoCapture(), CancellationToken.None);

        var logged = _wire.Server.LogEntries.Should().ContainSingle().Subject;
        logged.RequestMessage.Method.Should().Be("PUT");
        logged.RequestMessage.AbsolutePath.Should().Be($"/api/v1/projects/{ProjectId}/tasks");
        logged.RequestMessage.Headers!["Authorization"].ToString()
            .Should().Be($"Bearer {ApiToken}");
    }
}

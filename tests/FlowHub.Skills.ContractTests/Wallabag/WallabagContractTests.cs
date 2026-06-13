using System.Net;
using FlowHub.Core.Captures;
using FlowHub.Skills.Wallabag;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace FlowHub.Skills.ContractTests.Wallabag;

[Trait("Category", "SkillContract")]
public sealed class WallabagContractTests : IClassFixture<WireMockServerFixture>, IDisposable
{
    private const string ApiToken = "test-token";

    private readonly WireMockServerFixture _wire;
    private readonly HttpClient _http;
    private readonly HttpClient _tokenHttp;
    private readonly WallabagTokenProvider _tokenProvider;
    private readonly WallabagSkillIntegration _sut;

    public WallabagContractTests(WireMockServerFixture wire)
    {
        _wire = wire;
        _wire.Reset();

        // OAuth grant stub: the provider mints the access token the entries POST then carries.
        _wire.Server
            .Given(Request.Create().WithPath("/oauth/v2/token").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody($$"""{"access_token":"{{ApiToken}}","expires_in":3600,"token_type":"bearer","refresh_token":"r"}"""));

        var options = Options.Create(new WallabagOptions
        {
            BaseUrl = _wire.BaseUrl,
            ClientId = "client-id",
            ClientSecret = "client-secret",
            Username = "user",
            Password = "pass",
        });

        _tokenHttp = new HttpClient { BaseAddress = new Uri(_wire.BaseUrl) };
        _tokenProvider = new WallabagTokenProvider(
            _tokenHttp,
            options,
            TimeProvider.System,
            NullLogger<WallabagTokenProvider>.Instance);

        _http = new HttpClient { BaseAddress = new Uri(_wire.BaseUrl) };
        _sut = new WallabagSkillIntegration(
            _http,
            _tokenProvider,
            NullLogger<WallabagSkillIntegration>.Instance);
    }

    public void Dispose()
    {
        _http.Dispose();
        _tokenHttp.Dispose();
        _tokenProvider.Dispose();
    }

    private static Capture UrlCapture(string url = "https://en.wikipedia.org/wiki/Hexagonal_architecture") =>
        new(
            Id: Guid.NewGuid(),
            Source: ChannelKind.Web,
            Content: url,
            CreatedAt: DateTimeOffset.UtcNow,
            Stage: LifecycleStage.Classified,
            MatchedSkill: "Wallabag",
            Title: null);

    [Fact]
    public async Task HandleAsync_HappyPath_PostsEntryAndReturnsExternalRef()
    {
        const string url = "https://en.wikipedia.org/wiki/Hexagonal_architecture";
        _wire.Server
            .Given(Request.Create()
                .WithPath("/api/entries.json")
                .UsingPost()
                .WithHeader("Authorization", $"Bearer {ApiToken}")
                .WithBody(b => b is not null && b.Contains($"\"url\":\"{url}\"")))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"id":4242}"""));

        var result = await _sut.HandleAsync(UrlCapture(url), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.ExternalRef.Should().Be("4242");
    }

    [Fact]
    public async Task HandleAsync_NonUrlContent_ThrowsInvalidOperation()
    {
        var capture = UrlCapture(url: "todo: not a url");

        var act = () => _sut.HandleAsync(capture, CancellationToken.None);

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*not a valid absolute url*");
        _wire.Server.LogEntries.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_NonHttpScheme_ThrowsInvalidOperation()
    {
        var capture = UrlCapture(url: "ftp://files.example.com/readme.txt");

        var act = () => _sut.HandleAsync(capture, CancellationToken.None);

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*not a valid absolute url*");
        _wire.Server.LogEntries.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_ServerReturns401_ThrowsHttpRequestException()
    {
        _wire.Server
            .Given(Request.Create().WithPath("/api/entries.json").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(HttpStatusCode.Unauthorized));

        var act = () => _sut.HandleAsync(UrlCapture(), CancellationToken.None);

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task HandleAsync_ServerReturns500_ThrowsHttpRequestException()
    {
        _wire.Server
            .Given(Request.Create().WithPath("/api/entries.json").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(HttpStatusCode.InternalServerError));

        var act = () => _sut.HandleAsync(UrlCapture(), CancellationToken.None);

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task HandleAsync_ResponseBodyMissingId_ThrowsInvalidOperation()
    {
        _wire.Server
            .Given(Request.Create().WithPath("/api/entries.json").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBody("""{"title":"no id here"}"""));

        var act = () => _sut.HandleAsync(UrlCapture(), CancellationToken.None);

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*did not include an 'id' field*");
    }

    [Fact]
    public async Task HandleAsync_SendsBearerToken_OnExactPath()
    {
        _wire.Server
            .Given(Request.Create()
                .WithPath("/api/entries.json")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBodyAsJson(new { id = 1L }));

        await _sut.HandleAsync(UrlCapture(), CancellationToken.None);

        // The provider's grant POST is also logged; assert on the entries call specifically.
        var logged = _wire.Server.LogEntries
            .Should().ContainSingle(e => e.RequestMessage.AbsolutePath == "/api/entries.json").Subject;
        logged.RequestMessage.Method.Should().Be("POST");
        logged.RequestMessage.AbsolutePath.Should().Be("/api/entries.json");
        logged.RequestMessage.Headers!["Authorization"].ToString()
            .Should().Be($"Bearer {ApiToken}");
    }
}

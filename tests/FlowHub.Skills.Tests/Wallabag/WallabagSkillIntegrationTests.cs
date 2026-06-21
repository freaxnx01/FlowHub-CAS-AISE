using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using FlowHub.Core.Captures;
using FlowHub.Skills.Wallabag;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RichardSzalay.MockHttp;

namespace FlowHub.Skills.Tests.Wallabag;

public sealed class WallabagSkillIntegrationTests
{
    // Default resolver returns a fixed public IP so the success-path tests stay
    // deterministic and offline; the SSRF tests inject a private/throwing resolver.
    private static readonly Func<string, CancellationToken, Task<IPAddress[]>> PublicResolver =
        (_, _) => Task.FromResult<IPAddress[]>([IPAddress.Parse("93.184.216.34")]);

    private static (WallabagSkillIntegration sut, MockHttpMessageHandler mock) Build(
        WallabagOptions? options = null,
        Func<string, CancellationToken, Task<IPAddress[]>>? resolveHost = null)
    {
        options ??= new WallabagOptions
        {
            BaseUrl = "https://wallabag.example.com",
            ClientId = "client-id",
            ClientSecret = "client-secret",
            Username = "user",
            Password = "pass",
        };
        var mock = new MockHttpMessageHandler();
        var http = mock.ToHttpClient();
        http.BaseAddress = new Uri(options.BaseUrl!);

        // The token provider gets its OWN handler so the entries-handler's ordered
        // Expect()/VerifyNoOutstandingExpectation() queue never sees the grant POST.
        var tokenMock = new MockHttpMessageHandler();
        tokenMock.When(HttpMethod.Post, "*/oauth/v2/token")
            .Respond("application/json", """{"access_token":"test-token","expires_in":3600,"token_type":"bearer","refresh_token":"r"}""");
        var tokenHttp = tokenMock.ToHttpClient();
        tokenHttp.BaseAddress = new Uri(options.BaseUrl!);
        var tokenProvider = new WallabagTokenProvider(
            tokenHttp,
            Options.Create(options),
            TimeProvider.System,
            NullLogger<WallabagTokenProvider>.Instance);

        return (new WallabagSkillIntegration(http, tokenProvider, NullLogger<WallabagSkillIntegration>.Instance, resolveHost ?? PublicResolver), mock);
    }

    private static Capture UrlCapture(string url) => new(
        Id: Guid.NewGuid(),
        Source: ChannelKind.Web,
        Content: url,
        CreatedAt: DateTimeOffset.UtcNow,
        Stage: LifecycleStage.Classified,
        MatchedSkill: "Wallabag",
        Title: "Hexagonal architecture");

    [Fact]
    public void Name_IsWallabag()
    {
        var (sut, _) = Build();
        sut.Name.Should().Be("Wallabag");
    }

    [Fact]
    public async Task HandleAsync_ContentWithSurroundingText_ExtractsAndPostsTheUrl()
    {
        // Read-later captures arrive as free text, e.g. "save <url> to read later" —
        // the adapter must extract the URL from the content, not treat the whole
        // content as a URL (which throws InvalidOperationException).
        var (sut, mock) = Build();
        mock.Expect(HttpMethod.Post, "https://wallabag.example.com/api/entries.json")
            .WithPartialContent("\"url\":\"https://arxiv.org/abs/1706.03762\"")
            .Respond("application/json", """{"id":512}""");

        var capture = UrlCapture("Save https://arxiv.org/abs/1706.03762 to read later.");

        var result = await sut.HandleAsync(capture, default);

        result.Success.Should().BeTrue();
        result.ExternalRef.Should().Be("512");
        mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task HandleAsync_PostsCaptureContentAsUrl_WithBearerToken_ReturnsExternalRef()
    {
        var (sut, mock) = Build();
        mock.Expect(HttpMethod.Post, "https://wallabag.example.com/api/entries.json")
            .WithHeaders("Authorization", "Bearer test-token")
            .WithPartialContent("\"url\":\"https://en.wikipedia.org/wiki/Hexagonal_architecture\"")
            .Respond("application/json", """{"id":4711,"url":"https://en.wikipedia.org/wiki/Hexagonal_architecture","title":"Hexagonal architecture"}""");

        var capture = UrlCapture("https://en.wikipedia.org/wiki/Hexagonal_architecture");

        var result = await sut.HandleAsync(capture, default);

        result.Success.Should().BeTrue();
        result.ExternalRef.Should().Be("4711");
        mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task HandleAsync_201Created_AlsoTreatedAsSuccess()
    {
        var (sut, mock) = Build();
        mock.Expect(HttpMethod.Post, "*/api/entries.json")
            .Respond(HttpStatusCode.Created, "application/json", """{"id":99}""");

        var result = await sut.HandleAsync(UrlCapture("https://example.com"), default);

        result.Success.Should().BeTrue();
        result.ExternalRef.Should().Be("99");
    }

    [Fact]
    public async Task HandleAsync_ServerReturns401_ThrowsHttpRequestException()
    {
        var (sut, mock) = Build();
        mock.Expect(HttpMethod.Post, "*/api/entries.json").Respond(HttpStatusCode.Unauthorized);

        var act = () => sut.HandleAsync(UrlCapture("https://example.com"), default);

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task HandleAsync_ServerReturns503_ThrowsHttpRequestException()
    {
        var (sut, mock) = Build();
        mock.Expect(HttpMethod.Post, "*/api/entries.json").Respond(HttpStatusCode.ServiceUnavailable);

        var act = () => sut.HandleAsync(UrlCapture("https://example.com"), default);

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task HandleAsync_ServerReturnsBodyWithoutId_ThrowsInvalidOperationException()
    {
        var (sut, mock) = Build();
        mock.Expect(HttpMethod.Post, "*/api/entries.json")
            .Respond("application/json", """{"url":"https://example.com"}""");

        var act = () => sut.HandleAsync(UrlCapture("https://example.com"), default);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*id*");
    }

    [Fact]
    public async Task HandleAsync_NonUrlContent_ThrowsBeforeCallingServer()
    {
        var (sut, mock) = Build();

        // Don't expect any HTTP call — invalid input must short-circuit.
        var capture = UrlCapture("not a url at all");
        var act = () => sut.HandleAsync(capture, default);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*no http(s) url*");
        mock.GetMatchCount(mock.When("*")).Should().Be(0);
    }

    [Theory]
    [InlineData("http://169.254.169.254/latest/meta-data/")] // cloud metadata endpoint
    [InlineData("http://127.0.0.1/")]                        // loopback
    [InlineData("http://10.0.0.5/")]                         // RFC 1918
    [InlineData("http://172.16.0.1/")]                       // RFC 1918
    [InlineData("http://192.168.1.10/")]                     // RFC 1918
    [InlineData("http://100.64.0.1/")]                       // CGNAT
    [InlineData("http://[::1]/")]                            // IPv6 loopback
    public async Task HandleAsync_NonPublicIpLiteral_ThrowsBeforeCallingServer(string url)
    {
        // SSRF guard: an IP-literal pointing at the internal network or cloud metadata
        // must be refused before any token fetch or outbound call.
        var (sut, mock) = Build();

        var act = () => sut.HandleAsync(UrlCapture(url), default);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*non-public*");
        mock.GetMatchCount(mock.When("*")).Should().Be(0);
    }

    [Fact]
    public async Task HandleAsync_NamedHostResolvingToPrivateIp_ThrowsBeforeCallingServer()
    {
        // DNS-based SSRF: a public-looking host that resolves to an internal address.
        var (sut, mock) = Build(
            resolveHost: (_, _) => Task.FromResult<IPAddress[]>([IPAddress.Parse("10.1.2.3")]));

        var act = () => sut.HandleAsync(UrlCapture("http://intranet.attacker.example/"), default);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*non-public*");
        mock.GetMatchCount(mock.When("*")).Should().Be(0);
    }

    [Fact]
    public async Task HandleAsync_UnresolvableHost_FailsClosed()
    {
        // A host that cannot be resolved is treated as non-routable, not passed through.
        var (sut, mock) = Build(
            resolveHost: (_, _) => throw new SocketException());

        var act = () => sut.HandleAsync(UrlCapture("http://does-not-resolve.invalid/"), default);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*non-public*");
        mock.GetMatchCount(mock.When("*")).Should().Be(0);
    }

    [Fact]
    public async Task HandleAsync_PublicIpLiteral_IsAllowed()
    {
        // The allow-path for IP literals: a public address is classified directly
        // (no DNS) and proceeds to save. Covers the positive branch of IsPublic.
        var (sut, mock) = Build();
        mock.Expect(HttpMethod.Post, "https://wallabag.example.com/api/entries.json")
            .WithPartialContent("\"url\":\"http://1.1.1.1/\"")
            .Respond("application/json", """{"id":777}""");

        var result = await sut.HandleAsync(UrlCapture("http://1.1.1.1/"), default);

        result.Success.Should().BeTrue();
        result.ExternalRef.Should().Be("777");
        mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task HandleAsync_ObfuscatedDecimalIpLiteral_IsNotTreatedAsPublicLiteral()
    {
        // http://2130706433/ is the legacy decimal form of 127.0.0.1. Whether .NET
        // parses it as an IP literal or treats it as a name (it currently rejects the
        // legacy form, so it resolves), the guard must refuse the loopback it denotes.
        // The stub returns that loopback to lock the rejection against a future parser
        // change silently letting the obfuscated form through to Wallabag.
        var (sut, mock) = Build(
            resolveHost: (_, _) => Task.FromResult<IPAddress[]>([IPAddress.Parse("127.0.0.1")]));

        var act = () => sut.HandleAsync(UrlCapture("http://2130706433/"), default);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*non-public*");
        mock.GetMatchCount(mock.When("*")).Should().Be(0);
    }
}

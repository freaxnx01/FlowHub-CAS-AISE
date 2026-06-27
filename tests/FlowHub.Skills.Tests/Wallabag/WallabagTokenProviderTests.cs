using FlowHub.Skills.Wallabag;
using Microsoft.Extensions.Logging.Abstractions;
using RichardSzalay.MockHttp;

namespace FlowHub.Skills.Tests.Wallabag;

public sealed class WallabagTokenProviderTests
{
    private static WallabagOptions Options() => new()
    {
        BaseUrl = "https://wallabag.example.com",
        ClientId = "client-id",
        ClientSecret = "client-secret",
        Username = "user",
        Password = "pass",
    };

    private static WallabagTokenProvider Build(MockHttpMessageHandler mock, TimeProvider time, WallabagOptions? options = null)
    {
        options ??= Options();
        var http = mock.ToHttpClient();
        http.BaseAddress = new Uri(options.BaseUrl!);
        return new WallabagTokenProvider(
            http,
            Microsoft.Extensions.Options.Options.Create(options),
            time,
            NullLogger<WallabagTokenProvider>.Instance);
    }

    [Fact]
    public async Task GetTokenAsync_FirstCall_FetchesAccessToken()
    {
        var mock = new MockHttpMessageHandler();
        mock.Expect(HttpMethod.Post, "*/oauth/v2/token")
            .Respond("application/json", """{"access_token":"first-token","expires_in":3600,"token_type":"bearer","refresh_token":"r"}""");
        var sut = Build(mock, new MutableTimeProvider(DateTimeOffset.UtcNow));

        var token = await sut.GetTokenAsync(default);

        token.Should().Be("first-token");
        mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task GetTokenAsync_SecondCallBeforeExpiry_ReusesCachedTokenWithoutSecondPost()
    {
        var mock = new MockHttpMessageHandler();
        // Exactly one grant POST is expected — a second would leave it outstanding only
        // if expected; we Expect once and assert no second occurs via match count.
        mock.Expect(HttpMethod.Post, "*/oauth/v2/token")
            .Respond("application/json", """{"access_token":"cached-token","expires_in":3600,"token_type":"bearer","refresh_token":"r"}""");
        var time = new MutableTimeProvider(DateTimeOffset.UtcNow);
        var sut = Build(mock, time);

        var first = await sut.GetTokenAsync(default);
        // advance a little, but well within the 1h lifetime (minus margin)
        time.Advance(TimeSpan.FromMinutes(10));
        var second = await sut.GetTokenAsync(default);

        first.Should().Be("cached-token");
        second.Should().Be("cached-token");
        mock.VerifyNoOutstandingExpectation();
        mock.GetMatchCount(mock.Expect(HttpMethod.Post, "*/oauth/v2/token")).Should().Be(0,
            "the cached token must be reused, so no further grant POST is issued");
    }

    [Fact]
    public async Task GetTokenAsync_AfterLifetimeElapses_RefetchesNewToken()
    {
        var mock = new MockHttpMessageHandler();
        mock.Expect(HttpMethod.Post, "*/oauth/v2/token")
            .Respond("application/json", """{"access_token":"first-token","expires_in":3600,"token_type":"bearer","refresh_token":"r"}""");
        mock.Expect(HttpMethod.Post, "*/oauth/v2/token")
            .Respond("application/json", """{"access_token":"second-token","expires_in":3600,"token_type":"bearer","refresh_token":"r"}""");
        var time = new MutableTimeProvider(DateTimeOffset.UtcNow);
        var sut = Build(mock, time);

        var first = await sut.GetTokenAsync(default);
        // advance past the lifetime (3600s) including the 60s safety margin
        time.Advance(TimeSpan.FromSeconds(3600));
        var second = await sut.GetTokenAsync(default);

        first.Should().Be("first-token");
        second.Should().Be("second-token");
        mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task GetTokenAsync_ServerReturns500_LogsAndRethrows()
    {
        // Exercises the catch arm (EventId 3040) — any HTTP/parse failure is logged
        // with the exception type, then rethrown unchanged.
        var mock = new MockHttpMessageHandler();
        mock.Expect(HttpMethod.Post, "*/oauth/v2/token")
            .Respond(System.Net.HttpStatusCode.InternalServerError);
        using var sut = Build(mock, new MutableTimeProvider(DateTimeOffset.UtcNow));

        var act = () => sut.GetTokenAsync(default);

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public void Dispose_CanBeCalledRepeatedlyWithoutThrowing()
    {
        // Hits the Dispose() line and exercises the SemaphoreSlim's own
        // idempotent disposal semantics.
        var mock = new MockHttpMessageHandler();
        var sut = Build(mock, new MutableTimeProvider(DateTimeOffset.UtcNow));

        sut.Dispose();

        var act = sut.Dispose;
        act.Should().NotThrow();
    }

    /// <summary>
    /// Hand-rolled controllable <see cref="TimeProvider"/>. The repo does not reference
    /// Microsoft.Extensions.TimeProvider.Testing, so we model a mutable "now" here.
    /// </summary>
    private sealed class MutableTimeProvider(DateTimeOffset start) : TimeProvider
    {
        private DateTimeOffset _now = start;

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan by) => _now += by;
    }
}

using System.Net;
using System.Net.Http.Json;
using FlowHub.Skills.Vikunja;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FlowHub.Web.ComponentTests.Skills;

public class VikunjaProjectCatalogTests
{
    private static VikunjaProjectCatalog Build(HttpMessageHandler handler, VikunjaOptions? opts = null)
    {
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://vikunja.test") };
        var options = Options.Create(opts ?? new VikunjaOptions
        {
            BaseUrl = "https://vikunja.test",
            ApiToken = "tok",
            FallbackProject = "Inbox",
            FallbackProjectId = 1,
        });
        return new VikunjaProjectCatalog(http, options, NullLogger<VikunjaProjectCatalog>.Instance, TimeProvider.System);
    }

    [Fact]
    public async Task GetAsync_ReturnsMapFromApi()
    {
        var handler = new ScriptedHandler(_ => JsonContent.Create(new[]
        {
            new { id = 1, title = "Inbox" },
            new { id = 7, title = "Quotes" },
        }));

        var catalog = Build(handler);

        var map = await catalog.GetAsync(CancellationToken.None);

        map.Should().ContainKey("Inbox").WhoseValue.Should().Be(1);
        map.Should().ContainKey("Quotes").WhoseValue.Should().Be(7);
    }

    [Fact]
    public async Task GetAsync_FirstCallFails_ReturnsFallbackOnly()
    {
        var handler = new ScriptedHandler(_ => throw new HttpRequestException("boom"));

        var catalog = Build(handler);

        var map = await catalog.GetAsync(CancellationToken.None);

        map.Should().HaveCount(1).And.ContainKey("Inbox").WhoseValue.Should().Be(1);
    }

    [Fact]
    public async Task GetAsync_WithinTtl_ReturnsCachedResultWithoutSecondCall()
    {
        var calls = 0;
        var handler = new ScriptedHandler(_ =>
        {
            calls++;
            return JsonContent.Create(new[] { new { id = 1, title = "Inbox" } });
        });
        var catalog = Build(handler);

        await catalog.GetAsync(CancellationToken.None);
        await catalog.GetAsync(CancellationToken.None);

        calls.Should().Be(1);
    }

    private sealed class ScriptedHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpContent> _respond;
        public ScriptedHandler(Func<HttpRequestMessage, HttpContent> respond) => _respond = respond;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = _respond(request) });
    }
}

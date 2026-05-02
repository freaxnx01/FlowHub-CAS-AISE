using System.Net.Http.Json;
using System.Text.Json;

namespace FlowHub.Api.IntegrationTests.Captures;

public sealed class ProblemDetailsFormatTests : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;

    public ProblemDetailsFormatTests(IntegrationTestFactory factory) => _factory = factory;

    [Fact]
    public async Task ValidationFailure_HasRfc9457KeysAndTraceId()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/captures", new { content = "", source = "Api" });
        var json = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.TryGetProperty("type", out _).Should().BeTrue("type is required by RFC 9457");
        root.TryGetProperty("title", out _).Should().BeTrue();
        root.TryGetProperty("status", out _).Should().BeTrue();
        root.TryGetProperty("errors", out _).Should().BeTrue("validation failures flatten errors per field");
        // traceId is added by AddProblemDetails when an Activity is current; this should hold under WAF.
        root.TryGetProperty("traceId", out _).Should().BeTrue();
    }
}

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FlowHub.Core.Captures;

namespace FlowHub.Api.IntegrationTests.Captures;

public sealed class SubmitCaptureTests : IClassFixture<IntegrationTestFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
        PropertyNameCaseInsensitive = true,
    };

    private readonly IntegrationTestFactory _factory;

    public SubmitCaptureTests(IntegrationTestFactory factory) => _factory = factory;

    [Fact]
    public async Task Post_ValidContent_Returns201WithLocationAndCapture()
    {
        var client = _factory.CreateClient();
        var body = new { content = "https://example.com/article", source = "Api" };

        var response = await client.PostAsJsonAsync("/api/v1/captures", body);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().StartWith("/api/v1/captures/");

        var capture = await response.Content.ReadFromJsonAsync<Capture>(JsonOptions);
        capture.Should().NotBeNull();
        capture!.Content.Should().Be("https://example.com/article");
        capture.Source.Should().Be(ChannelKind.Api);
        capture.Stage.Should().Be(LifecycleStage.Raw);
    }

    [Fact]
    public async Task Post_EmptyContent_Returns400WithValidationProblem()
    {
        var client = _factory.CreateClient();
        var body = new { content = "", source = "Api" };

        var response = await client.PostAsJsonAsync("/api/v1/captures", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        var problem = await response.Content.ReadAsStringAsync();
        problem.Should().Contain("\"errors\"").And.Contain("Content");
    }

    [Fact]
    public async Task Post_ContentOver8192_Returns400()
    {
        var client = _factory.CreateClient();
        var body = new { content = new string('x', 8193), source = "Api" };

        var response = await client.PostAsJsonAsync("/api/v1/captures", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_UnknownSource_Returns400()
    {
        var client = _factory.CreateClient();
        var body = new { content = "ok", source = "DoesNotExist" };

        var response = await client.PostAsJsonAsync("/api/v1/captures", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}

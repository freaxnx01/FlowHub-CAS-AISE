using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FlowHub.Core.Captures;

namespace FlowHub.Api.IntegrationTests.Captures;

public sealed class GetCaptureByIdTests : IClassFixture<IntegrationTestFactory>
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly IntegrationTestFactory _factory;

    public GetCaptureByIdTests(IntegrationTestFactory factory) => _factory = factory;

    [Fact]
    public async Task Get_KnownId_Returns200WithCapture()
    {
        var client = _factory.CreateClient();
        var submit = await client.PostAsJsonAsync("/api/v1/captures",
            new { content = "for-getbyid", source = "Api" });
        var created = await submit.Content.ReadFromJsonAsync<Capture>(JsonOpts);

        var response = await client.GetAsync($"/api/v1/captures/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var capture = await response.Content.ReadFromJsonAsync<Capture>(JsonOpts);
        capture!.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task Get_UnknownId_Returns404WithCaptureNotFoundProblem()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/captures/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("capture-not-found");
    }
}

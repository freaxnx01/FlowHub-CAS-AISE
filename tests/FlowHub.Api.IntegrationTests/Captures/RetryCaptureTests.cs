using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FlowHub.Core.Captures;

namespace FlowHub.Api.IntegrationTests.Captures;

public sealed class RetryCaptureTests : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
    };

    public RetryCaptureTests(IntegrationTestFactory factory) => _factory = factory;

    [Fact]
    public async Task Retry_OrphanCapture_Returns202AndResetsStageToRaw()
    {
        var client = _factory.CreateClient();
        // The seeded Bogus stub includes captures at indices 2 and 8 with Stage=Orphan.
        var listResponse = await client.GetAsync("/api/v1/captures?stage=Orphan&limit=1");
        var page = await listResponse.Content.ReadFromJsonAsync<ListPage>(JsonOpts);
        page!.Items.Should().NotBeEmpty();
        var orphan = page.Items[0];

        var response = await client.PostAsync($"/api/v1/captures/{orphan.Id}/retry", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var capture = await response.Content.ReadFromJsonAsync<Capture>(JsonOpts);
        capture!.Stage.Should().Be(LifecycleStage.Raw);
        capture.FailureReason.Should().BeNull();
    }

    [Fact]
    public async Task Retry_CompletedCapture_Returns409WithNotRetryableProblem()
    {
        var client = _factory.CreateClient();
        var listResponse = await client.GetAsync("/api/v1/captures?stage=Completed&limit=1");
        var page = await listResponse.Content.ReadFromJsonAsync<ListPage>(JsonOpts);
        page!.Items.Should().NotBeEmpty();
        var completed = page.Items[0];

        var response = await client.PostAsync($"/api/v1/captures/{completed.Id}/retry", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("capture-not-retryable");
    }

    [Fact]
    public async Task Retry_UnknownId_Returns404()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsync($"/api/v1/captures/{Guid.NewGuid()}/retry", content: null);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private sealed record ListPage(IReadOnlyList<Capture> Items, string? NextCursor);
}

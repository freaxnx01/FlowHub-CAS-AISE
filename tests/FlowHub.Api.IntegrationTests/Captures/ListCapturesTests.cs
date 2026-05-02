using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FlowHub.Core.Captures;

namespace FlowHub.Api.IntegrationTests.Captures;

public sealed class ListCapturesTests : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
    };

    public ListCapturesTests(IntegrationTestFactory factory) => _factory = factory;

    [Fact]
    public async Task Get_NoFilter_ReturnsFirstPageWithItems()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/captures?limit=5");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<ListResponse>(JsonOpts);
        page.Should().NotBeNull();
        page!.Items.Should().HaveCountLessThanOrEqualTo(5);
        page.NextCursor.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Get_WithCursor_ReturnsNextPageNoOverlap()
    {
        var client = _factory.CreateClient();
        var firstResponse = await client.GetAsync("/api/v1/captures?limit=3");
        var firstPage = await firstResponse.Content.ReadFromJsonAsync<ListResponse>(JsonOpts);

        var secondResponse = await client.GetAsync($"/api/v1/captures?limit=3&cursor={Uri.EscapeDataString(firstPage!.NextCursor!)}");

        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var secondPage = await secondResponse.Content.ReadFromJsonAsync<ListResponse>(JsonOpts);
        secondPage!.Items.Should().NotBeEmpty();
        secondPage.Items.Select(c => c.Id).Should().NotIntersectWith(firstPage.Items.Select(c => c.Id));
    }

    [Fact]
    public async Task Get_StageFilter_ReturnsOnlyMatchingStages()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/captures?stage=Orphan,Unhandled&limit=50");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<ListResponse>(JsonOpts);
        page!.Items.Should().OnlyContain(c =>
            c.Stage == LifecycleStage.Orphan || c.Stage == LifecycleStage.Unhandled);
    }

    [Fact]
    public async Task Get_SourceFilter_ReturnsOnlyMatching()
    {
        var client = _factory.CreateClient();
        await client.PostAsJsonAsync("/api/v1/captures", new { content = "for-api-source-test", source = "Api" });

        var response = await client.GetAsync("/api/v1/captures?source=Api&limit=50");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<ListResponse>(JsonOpts);
        page!.Items.Should().OnlyContain(c => c.Source == ChannelKind.Api);
    }

    [Fact]
    public async Task Get_MalformedCursor_Returns400()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/captures?cursor=not-a-real-cursor");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    private sealed record ListResponse(IReadOnlyList<Capture> Items, string? NextCursor);
}

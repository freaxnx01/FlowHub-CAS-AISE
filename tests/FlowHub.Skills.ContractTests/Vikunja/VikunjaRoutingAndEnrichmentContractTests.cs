using FlowHub.Core.Captures;
using FlowHub.Core.Skills;
using FlowHub.Skills.Vikunja;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace FlowHub.Skills.ContractTests.Vikunja;

/// <summary>
/// End-to-end contract tests for the routing + enrichment pipeline introduced in PR #18:
/// real <see cref="VikunjaProjectCatalog"/> against WireMock + real <see cref="VikunjaSkillIntegration"/>
/// resolving the bucket → projectId via the live catalog and posting with the enriched description.
/// </summary>
[Trait("Category", "SkillContract")]
public sealed class VikunjaRoutingAndEnrichmentContractTests : IClassFixture<WireMockServerFixture>, IDisposable
{
    private const int InboxId = 1;
    private const int ZitateId = 7;
    private const string ApiToken = "test-token";

    private readonly WireMockServerFixture _wire;
    private readonly HttpClient _http;
    private readonly HttpClient _catalogHttp;
    private readonly VikunjaProjectCatalog _catalog;
    private readonly VikunjaSkillIntegration _sut;

    public VikunjaRoutingAndEnrichmentContractTests(WireMockServerFixture wire)
    {
        _wire = wire;
        _wire.Reset();

        var options = new VikunjaOptions
        {
            BaseUrl = _wire.BaseUrl,
            ApiToken = ApiToken,
            FallbackProject = "Inbox",
            FallbackProjectId = InboxId,
        };

        _catalogHttp = new HttpClient { BaseAddress = new Uri(_wire.BaseUrl) };
        _catalog = new VikunjaProjectCatalog(
            _catalogHttp,
            Options.Create(options),
            NullLogger<VikunjaProjectCatalog>.Instance,
            TimeProvider.System);

        _http = new HttpClient { BaseAddress = new Uri(_wire.BaseUrl) };
        _sut = new VikunjaSkillIntegration(
            _http,
            Options.Create(options),
            _catalog,
            NullLogger<VikunjaSkillIntegration>.Instance);
    }

    public void Dispose()
    {
        _http.Dispose();
        _catalogHttp.Dispose();
        _catalog.Dispose();
    }

    private void StubCatalog()
    {
        _wire.Server
            .Given(Request.Create()
                .WithPath("/api/v1/projects")
                .UsingGet()
                .WithHeader("Authorization", $"Bearer {ApiToken}"))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""[{"id":1,"title":"Inbox"},{"id":7,"title":"Zitate"},{"id":12,"title":"Movies"}]"""));
    }

    private static Capture QuoteCapture(string? enrichmentDescription = null) =>
        new(
            Id: Guid.NewGuid(),
            Source: ChannelKind.Web,
            Content: "\"Unix and C are the ultimate computer viruses.\", Richard Gabriel",
            CreatedAt: DateTimeOffset.UtcNow,
            Stage: LifecycleStage.Classified,
            MatchedSkill: "Vikunja",
            Title: "Gabriel on Unix and C",
            VikunjaProject: "Zitate",
            EnrichmentDescription: enrichmentDescription);

    [Fact]
    public async Task HandleAsync_ResolvesBucketViaCatalog_AndPutsTaskToCorrectProject()
    {
        StubCatalog();
        _wire.Server
            .Given(Request.Create()
                .WithPath($"/api/v1/projects/{ZitateId}/tasks")
                .UsingPut())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBodyAsJson(new { id = 555L }));

        var result = await _sut.HandleAsync(QuoteCapture(), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.ExternalRef.Should().Be("555");

        var puts = _wire.Server.LogEntries
            .Where(e => e.RequestMessage.Method == "PUT")
            .ToList();
        puts.Should().ContainSingle();
        puts[0].RequestMessage.AbsolutePath.Should().Be($"/api/v1/projects/{ZitateId}/tasks");
        puts[0].RequestMessage.AbsolutePath.Should().NotContain($"/projects/{InboxId}/");
    }

    [Fact]
    public async Task HandleAsync_PassesEnrichmentDescription_InRequestBody()
    {
        StubCatalog();
        const string bio = "**About Richard Gabriel:** American computer scientist; co-author of the 'Worse is Better' essay.";
        _wire.Server
            .Given(Request.Create()
                .WithPath($"/api/v1/projects/{ZitateId}/tasks")
                .UsingPut())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBodyAsJson(new { id = 556L }));

        await _sut.HandleAsync(QuoteCapture(enrichmentDescription: bio), CancellationToken.None);

        var put = _wire.Server.LogEntries
            .Single(e => e.RequestMessage.Method == "PUT");
        var body = put.RequestMessage.Body!;
        body.Should().Contain("\"title\":\"Gabriel on Unix and C\"");
        body.Should().Contain("\"description\":");
        body.Should().Contain("Richard Gabriel");
        body.Should().Contain("computer scientist");
    }

    [Fact]
    public async Task HandleAsync_UnknownBucketName_FallsBackToInboxProjectId()
    {
        StubCatalog();
        _wire.Server
            .Given(Request.Create()
                .WithPath($"/api/v1/projects/{InboxId}/tasks")
                .UsingPut())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBodyAsJson(new { id = 1L }));

        var capture = QuoteCapture() with { VikunjaProject = "ProjectThatDoesNotExist" };

        var result = await _sut.HandleAsync(capture, CancellationToken.None);

        result.Success.Should().BeTrue();
        var put = _wire.Server.LogEntries
            .Single(e => e.RequestMessage.Method == "PUT");
        put.RequestMessage.AbsolutePath.Should().Be($"/api/v1/projects/{InboxId}/tasks");
    }

    [Fact]
    public async Task HandleAsync_CallsCatalogOnce_AcrossMultipleInvocations()
    {
        StubCatalog();
        _wire.Server
            .Given(Request.Create()
                .WithPath($"/api/v1/projects/{ZitateId}/tasks")
                .UsingPut())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBodyAsJson(new { id = 1L }));

        await _sut.HandleAsync(QuoteCapture(), CancellationToken.None);
        await _sut.HandleAsync(QuoteCapture(), CancellationToken.None);
        await _sut.HandleAsync(QuoteCapture(), CancellationToken.None);

        var catalogGets = _wire.Server.LogEntries
            .Where(e => e.RequestMessage.Method == "GET"
                     && e.RequestMessage.AbsolutePath == "/api/v1/projects")
            .ToList();
        catalogGets.Should().ContainSingle("the TTL cache should serve subsequent calls without refetching");
    }
}

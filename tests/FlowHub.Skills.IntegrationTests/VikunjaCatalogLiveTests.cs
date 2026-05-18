using FlowHub.Core.Captures;
using FlowHub.Skills.Vikunja;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FlowHub.Skills.IntegrationTests;

/// <summary>
/// Live integration tests for the per-Vikunja-project routing introduced in PR #18.
/// Hits a real Vikunja instance via Skills__Vikunja__* env vars and verifies:
///   1. the canonical routing buckets (Inbox, Quotes, Movies, Ausflugziele) are
///      present in the live catalog — fails loudly if a bucket is missing so the
///      operator knows to create it before the production routing pipeline runs;
///   2. an end-to-end Quotes route lands a task in the correct project, resolved
///      live via the catalog (no stub).
///
/// Run via `make test-services` (alias `test-beta`). Skipped without env vars.
/// </summary>
[Trait("Category", "BetaSmoke")]
public sealed class VikunjaCatalogLiveTests
{
    private static readonly string[] CanonicalBuckets = ["Inbox", "Quotes", "Movies", "Ausflugziele"];

    private static (VikunjaOptions options, HttpClient catalogHttp, HttpClient skillHttp, VikunjaProjectCatalog catalog) Build()
    {
        var baseUrl = Environment.GetEnvironmentVariable("Skills__Vikunja__BaseUrl");
        var token = Environment.GetEnvironmentVariable("Skills__Vikunja__ApiToken");
        var projectIdRaw = Environment.GetEnvironmentVariable("Skills__Vikunja__FallbackProjectId");
        Skip.If(
            string.IsNullOrWhiteSpace(baseUrl)
                || string.IsNullOrWhiteSpace(token)
                || string.IsNullOrWhiteSpace(projectIdRaw),
            "Skills__Vikunja__BaseUrl/ApiToken/FallbackProjectId not configured");

        var projectId = int.Parse(projectIdRaw!, System.Globalization.CultureInfo.InvariantCulture);
        var options = new VikunjaOptions
        {
            BaseUrl = baseUrl,
            ApiToken = token,
            FallbackProject = "Inbox",
            FallbackProjectId = projectId,
        };

        var catalogHttp = new HttpClient { BaseAddress = new Uri(baseUrl!), Timeout = TimeSpan.FromSeconds(15) };
        var catalog = new VikunjaProjectCatalog(
            catalogHttp,
            Options.Create(options),
            NullLogger<VikunjaProjectCatalog>.Instance,
            TimeProvider.System);

        var skillHttp = new HttpClient { BaseAddress = new Uri(baseUrl!), Timeout = TimeSpan.FromSeconds(15) };

        return (options, catalogHttp, skillHttp, catalog);
    }

    [SkippableFact]
    public async Task Catalog_LiveVikunja_ContainsCanonicalRoutingBuckets()
    {
        var (_, catalogHttp, skillHttp, catalog) = Build();
        using var _h1 = catalogHttp;
        using var _h2 = skillHttp;
        using var _c = catalog;

        var map = await catalog.GetAsync(default);

        map.Should().NotBeEmpty("the live catalog must return at least one project");

        // Surface every missing canonical bucket at once so the operator can fix them
        // in a single pass rather than discovering them one by one across test runs.
        var missing = CanonicalBuckets.Where(b => !map.ContainsKey(b)).ToArray();
        missing.Should().BeEmpty(
            $"the live Vikunja instance is missing the routing buckets: [{string.Join(", ", missing)}]. " +
            "Create them in the Vikunja UI (or via API) before the production routing pipeline can route to them.");

        // Sanity: ids are positive integers.
        foreach (var bucket in CanonicalBuckets)
        {
            map[bucket].Should().BeGreaterThan(0, $"projectId for '{bucket}' must be a valid Vikunja project id");
        }
    }

    [SkippableFact]
    public async Task HandleAsync_LiveVikunja_RoutesQuoteCaptureToQuotesProject()
    {
        var (options, catalogHttp, skillHttp, catalog) = Build();
        using var _h1 = catalogHttp;
        using var _h2 = skillHttp;
        using var _c = catalog;

        // Pre-flight: skip cleanly if the Quotes bucket isn't provisioned yet —
        // the catalog presence test above is the canonical signal for missing buckets.
        var map = await catalog.GetAsync(default);
        Skip.If(!map.ContainsKey("Quotes"),
            "Live Vikunja instance has no 'Quotes' project — see Catalog_LiveVikunja_ContainsCanonicalRoutingBuckets.");

        var sut = new VikunjaSkillIntegration(
            skillHttp,
            Options.Create(options),
            catalog,
            NullLogger<VikunjaSkillIntegration>.Instance);

        var stamp = DateTimeOffset.UtcNow.ToString("O", System.Globalization.CultureInfo.InvariantCulture);
        var capture = new Capture(
            Id: Guid.NewGuid(),
            Source: ChannelKind.Web,
            Content: $"\"Unix and C are the ultimate computer viruses.\", Richard Gabriel — smoke {stamp}",
            CreatedAt: DateTimeOffset.UtcNow,
            Stage: LifecycleStage.Classified,
            MatchedSkill: "Vikunja",
            Title: $"Gabriel on Unix and C — smoke {stamp}",
            VikunjaProject: "Quotes",
            EnrichmentDescription:
                "> \"Unix and C are the ultimate computer viruses.\" — Richard Gabriel\n\n" +
                "**About Richard Gabriel:** American computer scientist; author of the 'Worse is Better' essay. (smoke-test bio)");

        var result = await sut.HandleAsync(capture, default);

        result.Success.Should().BeTrue();
        result.ExternalRef.Should().NotBeNullOrWhiteSpace(
            "the routed task must come back with a Vikunja-assigned external id");
    }
}

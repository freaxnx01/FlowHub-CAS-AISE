using FlowHub.AI;
using FlowHub.Core.Classification;

namespace FlowHub.Web.ComponentTests.Ai;

public class EnricherBucketCatalogTests
{
    private sealed class FakeEnricher(string bucket) : IEnricher
    {
        public string BucketName { get; } = bucket;

        public Task<EnrichmentResult?> EnrichAsync(
            Capture capture, ClassificationResult classification, CancellationToken cancellationToken) =>
            Task.FromResult<EnrichmentResult?>(null);
    }

    [Fact]
    public async Task Catalog_ExposesFallback_AndRegisteredEnricherBuckets()
    {
        // This is what lets the public demo route a quote to "Zitate" and run the
        // ZitateEnricher without a live Vikunja catalog.
        var catalog = new EnricherBucketCatalog(new IEnricher[] { new FakeEnricher("Zitate") }, "Inbox");

        var buckets = await catalog.GetAsync(CancellationToken.None);

        buckets.Keys.Should().Contain("Inbox");
        buckets.Keys.Should().Contain("Zitate");
    }
}

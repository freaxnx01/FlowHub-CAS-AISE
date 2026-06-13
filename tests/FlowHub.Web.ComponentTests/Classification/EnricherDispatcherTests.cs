using FlowHub.AI;
using FlowHub.Core.Captures;
using FlowHub.Core.Classification;
using FlowHub.Core.Skills;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace FlowHub.Web.ComponentTests.Classification;

public class EnricherDispatcherTests
{
    private static Capture Sample() => new(
        Guid.NewGuid(), ChannelKind.Web, "content", DateTimeOffset.UtcNow,
        LifecycleStage.Raw, "Vikunja");

    private static IVikunjaProjectCatalog Catalog(params (string Name, int Id)[] buckets)
    {
        var sub = Substitute.For<IVikunjaProjectCatalog>();
        sub.GetAsync(Arg.Any<CancellationToken>())
            .Returns(buckets.ToDictionary(b => b.Name, b => b.Id));
        return sub;
    }

    [Fact]
    public async Task DispatchAsync_NoEnricherForBucket_ReturnsNullAndUnchangedProject()
    {
        var dispatcher = new EnricherDispatcher(
            Array.Empty<IEnricher>(),
            Catalog(("Zitate", 7)),
            new VikunjaFallback("Inbox", 1),
            NullLogger<EnricherDispatcher>.Instance);

        var classification = new ClassificationResult(["t"], "Vikunja", "title", "Zitate");

        var (project, enrichment) = await dispatcher.DispatchAsync(Sample(), classification, default);

        project.Should().Be("Zitate");
        enrichment.Should().BeNull();
    }

    [Fact]
    public async Task DispatchAsync_UnknownProject_CoercedToFallback()
    {
        var dispatcher = new EnricherDispatcher(
            Array.Empty<IEnricher>(),
            Catalog(("Inbox", 1)),
            new VikunjaFallback("Inbox", 1),
            NullLogger<EnricherDispatcher>.Instance);

        var classification = new ClassificationResult(["t"], "Vikunja", "title", "DoesNotExist");

        var (project, enrichment) = await dispatcher.DispatchAsync(Sample(), classification, default);

        project.Should().Be("Inbox");
        enrichment.Should().BeNull();
    }

    [Fact]
    public async Task DispatchAsync_MatchingEnricher_RunsAndReturnsResult()
    {
        var enricher = Substitute.For<IEnricher>();
        enricher.BucketName.Returns("Zitate");
        enricher.EnrichAsync(Arg.Any<Capture>(), Arg.Any<ClassificationResult>(), Arg.Any<CancellationToken>())
            .Returns(new EnrichmentResult("**desc**"));

        var dispatcher = new EnricherDispatcher(
            new[] { enricher },
            Catalog(("Zitate", 7)),
            new VikunjaFallback("Inbox", 1),
            NullLogger<EnricherDispatcher>.Instance);

        var (project, enrichment) = await dispatcher.DispatchAsync(
            Sample(), new ClassificationResult(["t"], "Vikunja", "title", "Zitate"), default);

        project.Should().Be("Zitate");
        enrichment!.Description.Should().Be("**desc**");
    }

    [Fact]
    public async Task DispatchAsync_EnricherThrows_ReturnsNullAndLogs()
    {
        var enricher = Substitute.For<IEnricher>();
        enricher.BucketName.Returns("Zitate");
        enricher.EnrichAsync(Arg.Any<Capture>(), Arg.Any<ClassificationResult>(), Arg.Any<CancellationToken>())
            .Returns<EnrichmentResult?>(_ => throw new InvalidOperationException("LLM down"));

        var dispatcher = new EnricherDispatcher(
            new[] { enricher },
            Catalog(("Zitate", 7)),
            new VikunjaFallback("Inbox", 1),
            NullLogger<EnricherDispatcher>.Instance);

        var (project, enrichment) = await dispatcher.DispatchAsync(
            Sample(), new ClassificationResult(["t"], "Vikunja", "title", "Zitate"), default);

        project.Should().Be("Zitate");
        enrichment.Should().BeNull();
    }
}

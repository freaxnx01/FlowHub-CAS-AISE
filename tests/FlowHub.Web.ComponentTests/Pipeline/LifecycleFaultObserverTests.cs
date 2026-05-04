using FlowHub.Core.Captures;
using FlowHub.Core.Classification;
using FlowHub.Core.Events;
using FlowHub.Core.Skills;
using FlowHub.Web.Pipeline;
using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace FlowHub.Web.ComponentTests.Pipeline;

public sealed class LifecycleFaultObserverTests
{
    [Fact]
    public async Task EnrichmentExhaustedRetries_MarksOrphan()
    {
        var classifier = Substitute.For<IClassifier>();
        classifier.ClassifyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<ClassificationResult>(_ => throw new InvalidOperationException("boom"));

        await using var provider = PipelineTestBase.Build(
            configure: s => s.AddSingleton(classifier),
            configureBus: x =>
            {
                x.AddConsumer<CaptureEnrichmentConsumer>(c =>
                    c.UseMessageRetry(r => r.Intervals(10, 10)));
                x.AddConsumer<LifecycleFaultObserver>();
            });

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var captureService = provider.GetRequiredService<ICaptureService>();
        var capture = await captureService.SubmitAsync("hello", ChannelKind.Web, default);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        (await harness.Consumed.Any<Fault<CaptureCreated>>(
            x => x.Context.Message.Message.CaptureId == capture.Id,
            cts.Token))
            .Should().BeTrue();

        (await captureService.GetByIdAsync(capture.Id, default))!.Stage.Should().Be(LifecycleStage.Orphan);
    }

    [Fact]
    public async Task RoutingExhaustedRetries_MarksUnhandled()
    {
        var integration = Substitute.For<ISkillIntegration>();
        integration.Name.Returns("Wallabag");
        integration.HandleAsync(Arg.Any<Capture>(), Arg.Any<CancellationToken>())
            .Returns<Task<SkillResult>>(_ => throw new InvalidOperationException("integration down"));

        await using var provider = PipelineTestBase.Build(
            configure: s => s.AddSingleton(integration),
            configureBus: x =>
            {
                x.AddConsumer<SkillRoutingConsumer>(c =>
                    c.UseMessageRetry(r => r.Intervals(10, 10, 10)));
                x.AddConsumer<LifecycleFaultObserver>();
            });

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var captureService = provider.GetRequiredService<ICaptureService>();
        var capture = await captureService.SubmitAsync("https://example.com", ChannelKind.Web, default);
        await captureService.MarkClassifiedAsync(capture.Id, "Wallabag", default);

        await harness.Bus.Publish(new CaptureClassified(
            capture.Id, ["link"], "Wallabag", DateTimeOffset.UtcNow));

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        (await harness.Consumed.Any<Fault<CaptureClassified>>(
            x => x.Context.Message.Message.CaptureId == capture.Id,
            cts.Token))
            .Should().BeTrue();

        (await captureService.GetByIdAsync(capture.Id, default))!.Stage.Should().Be(LifecycleStage.Unhandled);
    }
}

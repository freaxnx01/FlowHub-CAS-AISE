using FlowHub.Core.Captures;
using FlowHub.Core.Events;
using FlowHub.Core.Skills;
using FlowHub.Web.Pipeline;
using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace FlowHub.Web.ComponentTests.Pipeline;

public sealed class SkillRoutingConsumerTests
{
    [Fact]
    public async Task Consume_KnownSkill_HandleAsyncSucceeds_MarksCompletedWithExternalRef()
    {
        var integration = Substitute.For<ISkillIntegration>();
        integration.Name.Returns("Wallabag");
        integration.HandleAsync(Arg.Any<Capture>(), Arg.Any<CancellationToken>())
            .Returns(new SkillResult(Success: true, ExternalRef: "wal-42"));

        await using var provider = PipelineTestBase.Build(
            configure: s => s.AddSingleton(integration),
            configureBus: x => x.AddConsumer<SkillRoutingConsumer>());

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var captureService = provider.GetRequiredService<ICaptureService>();
        var capture = await captureService.SubmitAsync("https://example.com", ChannelKind.Web, default);
        await captureService.MarkClassifiedAsync(capture.Id, "Wallabag", title: null, default);

        await harness.Bus.Publish(new CaptureClassified(
            capture.Id, ["link"], "Wallabag", DateTimeOffset.UtcNow));

        (await harness.Consumed.Any<CaptureClassified>(
            x => x.Context.Message.CaptureId == capture.Id))
            .Should().BeTrue();

        await integration.Received(1).HandleAsync(
            Arg.Is<Capture>(c => c.Id == capture.Id),
            Arg.Any<CancellationToken>());

        var stored = await captureService.GetByIdAsync(capture.Id, default);
        stored!.Stage.Should().Be(LifecycleStage.Completed);
        stored.ExternalRef.Should().Be("wal-42");
    }

    [Fact]
    public async Task Consume_UnknownSkill_DoesNotCallIntegration_MarksUnhandled()
    {
        var integration = Substitute.For<ISkillIntegration>();
        integration.Name.Returns("Wallabag");

        await using var provider = PipelineTestBase.Build(
            configure: s => s.AddSingleton(integration),
            configureBus: x => x.AddConsumer<SkillRoutingConsumer>());

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var captureService = provider.GetRequiredService<ICaptureService>();
        var capture = await captureService.SubmitAsync("hello", ChannelKind.Web, default);
        await captureService.MarkClassifiedAsync(capture.Id, "DoesNotExist", title: null, default);

        await harness.Bus.Publish(new CaptureClassified(
            capture.Id, ["unknown"], "DoesNotExist", DateTimeOffset.UtcNow));

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        (await harness.Consumed.Any<CaptureClassified>(cts.Token)).Should().BeTrue();

        await integration.DidNotReceive().HandleAsync(Arg.Any<Capture>(), Arg.Any<CancellationToken>());

        (await captureService.GetByIdAsync(capture.Id, default))!.Stage.Should().Be(LifecycleStage.Unhandled);
    }

    [Fact]
    public async Task Consume_KnownSkill_HandleAsyncReturnsFailure_RetriesThenFault()
    {
        // !Success → consumer throws → MassTransit retries (3 attempts, but each returns
        // Success=false → eventually Fault<CaptureClassified>). The fault observer is wired
        // in PipelineTestBase via AddConsumer<LifecycleFaultObserver>().
        var integration = Substitute.For<ISkillIntegration>();
        integration.Name.Returns("Wallabag");
        integration.HandleAsync(Arg.Any<Capture>(), Arg.Any<CancellationToken>())
            .Returns(new SkillResult(Success: false, FailureReason: "wallabag returned 422"));

        await using var provider = PipelineTestBase.Build(
            configure: s => s.AddSingleton(integration),
            configureBus: x =>
            {
                x.AddConsumer<SkillRoutingConsumer>(c => c.UseMessageRetry(r => r.Immediate(2)));
                x.AddConsumer<LifecycleFaultObserver>();
            });

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var captureService = provider.GetRequiredService<ICaptureService>();
        var capture = await captureService.SubmitAsync("https://example.com", ChannelKind.Web, default);
        await captureService.MarkClassifiedAsync(capture.Id, "Wallabag", title: null, default);

        await harness.Bus.Publish(new CaptureClassified(
            capture.Id, ["link"], "Wallabag", DateTimeOffset.UtcNow));

        // Wait for fault observer to consume Fault<CaptureClassified>
        (await harness.Consumed.Any<Fault<CaptureClassified>>()).Should().BeTrue();

        var stored = await captureService.GetByIdAsync(capture.Id, default);
        stored!.Stage.Should().Be(LifecycleStage.Unhandled);
        stored.FailureReason.Should().Contain("wallabag returned 422");
    }
}

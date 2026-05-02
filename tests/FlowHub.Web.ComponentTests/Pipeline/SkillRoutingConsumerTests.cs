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
    public async Task Consume_KnownSkill_CallsIntegrationAndMarksRouted()
    {
        var integration = Substitute.For<ISkillIntegration>();
        integration.Name.Returns("Wallabag");
        integration.WriteAsync(Arg.Any<Capture>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        await using var provider = PipelineTestBase.Build(
            configure: s => s.AddSingleton(integration),
            configureBus: x => x.AddConsumer<SkillRoutingConsumer>());

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var captureService = provider.GetRequiredService<ICaptureService>();
        var capture = await captureService.SubmitAsync("https://example.com", ChannelKind.Web, default);
        await captureService.MarkClassifiedAsync(capture.Id, "Wallabag", default);

        await harness.Bus.Publish(new CaptureClassified(
            capture.Id, ["link"], "Wallabag", DateTimeOffset.UtcNow));

        (await harness.Consumed.Any<CaptureClassified>(
            x => x.Context.Message.CaptureId == capture.Id))
            .Should().BeTrue();

        await integration.Received(1).WriteAsync(
            Arg.Is<Capture>(c => c.Id == capture.Id),
            Arg.Any<IReadOnlyList<string>>(),
            Arg.Any<CancellationToken>());

        (await captureService.GetByIdAsync(capture.Id, default))!.Stage.Should().Be(LifecycleStage.Routed);
    }

    [Fact]
    public async Task Consume_UnknownSkill_MarksUnhandled()
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
        await captureService.MarkClassifiedAsync(capture.Id, "DoesNotExist", default);

        await harness.Bus.Publish(new CaptureClassified(
            capture.Id, ["unknown"], "DoesNotExist", DateTimeOffset.UtcNow));

        (await harness.Consumed.Any<CaptureClassified>()).Should().BeTrue();

        await integration.DidNotReceive().WriteAsync(
            Arg.Any<Capture>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>());

        (await captureService.GetByIdAsync(capture.Id, default))!.Stage.Should().Be(LifecycleStage.Unhandled);
    }
}

using FlowHub.Core.Captures;
using FlowHub.Core.Classification;
using FlowHub.Core.Events;
using FlowHub.Web.Pipeline;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace FlowHub.Web.ComponentTests.Pipeline;

public sealed class CaptureEnrichmentConsumerTests
{
    [Fact]
    public async Task Consume_UrlContent_PublishesCaptureClassifiedAndMarksClassified()
    {
        await using var provider = PipelineTestBase.Build(
            configure: s => s.AddSingleton(StubClassifier(new ClassificationResult(["link"], "Wallabag"))),
            configureBus: x => x.AddConsumer<CaptureEnrichmentConsumer>());

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var captureService = provider.GetRequiredService<ICaptureService>();
        var capture = await captureService.SubmitAsync("https://example.com", ChannelKind.Web, default);

        (await harness.Consumed.Any<CaptureCreated>(
            x => x.Context.Message.CaptureId == capture.Id))
            .Should().BeTrue();

        (await harness.Published.Any<CaptureClassified>(
            x => x.Context.Message.CaptureId == capture.Id
                && x.Context.Message.MatchedSkill == "Wallabag"))
            .Should().BeTrue();

        var stored = await captureService.GetByIdAsync(capture.Id, default);
        stored!.Stage.Should().Be(LifecycleStage.Classified);
        stored.MatchedSkill.Should().Be("Wallabag");
    }

    [Fact]
    public async Task Consume_EmptyClassification_MarksOrphanWithoutPublishingClassified()
    {
        await using var provider = PipelineTestBase.Build(
            configure: s => s.AddSingleton(StubClassifier(new ClassificationResult(["unsorted"], string.Empty))),
            configureBus: x => x.AddConsumer<CaptureEnrichmentConsumer>());

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var captureService = provider.GetRequiredService<ICaptureService>();
        var capture = await captureService.SubmitAsync("plain text", ChannelKind.Web, default);

        (await harness.Consumed.Any<CaptureCreated>(
            x => x.Context.Message.CaptureId == capture.Id))
            .Should().BeTrue();

        (await harness.Published.Any<CaptureClassified>())
            .Should().BeFalse();

        var stored = await captureService.GetByIdAsync(capture.Id, default);
        stored!.Stage.Should().Be(LifecycleStage.Orphan);
    }

    [Fact]
    public async Task Consume_KnownSkillWithTitle_PassesTitleToCaptureService()
    {
        var classifier = Substitute.For<IClassifier>();
        classifier.ClassifyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ClassificationResult(["link"], "Wallabag", Title: "Hexagonal architecture"));

        await using var provider = PipelineTestBase.Build(
            configure: s => s.AddSingleton(classifier),
            configureBus: x => x.AddConsumer<CaptureEnrichmentConsumer>());

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var captureService = provider.GetRequiredService<ICaptureService>();
        var capture = await captureService.SubmitAsync("https://example.com", ChannelKind.Web, default);

        (await harness.Consumed.Any<CaptureCreated>(
            x => x.Context.Message.CaptureId == capture.Id))
            .Should().BeTrue();

        var updated = await captureService.GetByIdAsync(capture.Id, default);
        updated!.Title.Should().Be("Hexagonal architecture");
    }

    private static IClassifier StubClassifier(ClassificationResult result)
    {
        var sub = Substitute.For<IClassifier>();
        sub.ClassifyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(result);
        return sub;
    }
}

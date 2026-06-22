using FlowHub.Core.Events;
using FlowHub.Web.Pipeline;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace FlowHub.Web.ComponentTests.Pipeline;

public sealed class CaptureEmbeddingConsumerTests
{
    [Fact]
    public async Task Consume_EmbeddingProvided_StoresEmbeddingOnRepository()
    {
        var emb = new float[] { 0.1f, 0.2f, 0.3f };
        var embeddings = Substitute.For<IEmbeddingService>();
        embeddings.GenerateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult<float[]?>(emb));
        var repo = Substitute.For<ICaptureRepository>();
        repo.StoreEmbeddingAsync(Arg.Any<Guid>(), Arg.Any<float[]>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        await using var sp = BuildHarness(embeddings, repo);
        var harness = sp.GetRequiredService<ITestHarness>();
        await harness.Start();

        var id = Guid.NewGuid();
        await sp.GetRequiredService<IBus>().Publish(
            new CaptureCreated(id, "hello", ChannelKind.Api, DateTimeOffset.UtcNow));

        (await harness.Consumed.Any<CaptureCreated>(x => x.Context.Message.CaptureId == id))
            .Should().BeTrue();

        await repo.Received(1).StoreEmbeddingAsync(id, Arg.Is<float[]>(v => v.SequenceEqual(emb)), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consume_NoEmbeddingProvided_LogsSkippedWithoutStoring()
    {
        var embeddings = Substitute.For<IEmbeddingService>();
        embeddings.GenerateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                  .Returns(Task.FromResult<float[]?>(null));
        var repo = Substitute.For<ICaptureRepository>();

        await using var sp = BuildHarness(embeddings, repo);
        var harness = sp.GetRequiredService<ITestHarness>();
        await harness.Start();

        var id = Guid.NewGuid();
        await sp.GetRequiredService<IBus>().Publish(
            new CaptureCreated(id, "hello", ChannelKind.Api, DateTimeOffset.UtcNow));

        (await harness.Consumed.Any<CaptureCreated>(x => x.Context.Message.CaptureId == id))
            .Should().BeTrue();

        await repo.DidNotReceive().StoreEmbeddingAsync(
            Arg.Any<Guid>(), Arg.Any<float[]>(), Arg.Any<CancellationToken>());
    }

    private static ServiceProvider BuildHarness(IEmbeddingService embeddings, ICaptureRepository repo)
    {
        var services = new ServiceCollection();
        services.AddSingleton(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(Microsoft.Extensions.Logging.ILogger<>),
            typeof(Microsoft.Extensions.Logging.Abstractions.NullLogger<>));
        services.AddSingleton(embeddings);
        services.AddSingleton(repo);
        services.AddMassTransitTestHarness(x => x.AddConsumer<CaptureEmbeddingConsumer>());
        return services.BuildServiceProvider(true);
    }
}

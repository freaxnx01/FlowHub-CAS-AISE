using FlowHub.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute.ExceptionExtensions;

namespace FlowHub.Web.ComponentTests.Ai;

public sealed class EmbeddingServiceTests
{
    [Fact]
    public async Task GenerateAsync_WhenGeneratorSucceeds_ReturnsFloatArray()
    {
        var expected = new float[] { 0.1f, 0.2f, 0.3f };
        var generator = Substitute.For<IEmbeddingGenerator<string, Embedding<float>>>();
        generator
            .GenerateAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<EmbeddingGenerationOptions?>(), Arg.Any<CancellationToken>())
            .Returns(new GeneratedEmbeddings<Embedding<float>>([new Embedding<float>(expected)]));

        var sut = new AiEmbeddingService(generator, NullLogger<AiEmbeddingService>.Instance);

        var result = await sut.GenerateAsync("hello world");

        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GenerateAsync_WhenGeneratorThrows_ReturnsNull()
    {
        var generator = Substitute.For<IEmbeddingGenerator<string, Embedding<float>>>();
        generator
            .GenerateAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<EmbeddingGenerationOptions?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("timeout"));

        var sut = new AiEmbeddingService(generator, NullLogger<AiEmbeddingService>.Instance);

        var result = await sut.GenerateAsync("hello world");

        result.Should().BeNull();
    }
}

using FlowHub.Core.Captures;
using MassTransit;

namespace FlowHub.Persistence.Tests;

public class EfCaptureServiceAttachmentTests
{
    [Fact]
    public async Task SubmitAsync_WithAttachment_PersistsAttachmentAndUsesFileNameAsContent()
    {
        var repo = Substitute.For<ICaptureRepository>();
        repo.AddAsync(Arg.Any<Capture>(), Arg.Any<CancellationToken>())
            .Returns(ci => Task.FromResult(ci.Arg<Capture>()));
        var storage = Substitute.For<IAttachmentStorage>();
        storage.SaveAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("2026/05/abc123.pdf");
        var publish = Substitute.For<IPublishEndpoint>();
        var sut = new EfCaptureService(repo, publish, storage);

        using var bytes = new MemoryStream(new byte[10]);
        var input = new AttachmentInput { Content = bytes, FileName = "invoice.pdf", ContentType = "application/pdf", SizeBytes = 10 };

        var capture = await sut.SubmitAsync(content: "ignored typed text", ChannelKind.Web, input);

        capture.Content.Should().Be("invoice.pdf");
        capture.Attachment.Should().NotBeNull();
        capture.Attachment!.RelativePath.Should().Be("2026/05/abc123.pdf");
        capture.Attachment.SizeBytes.Should().Be(10);
    }

    [Fact]
    public async Task SubmitAsync_WithAttachment_RepositoryThrows_DeletesStoredFile()
    {
        var repo = Substitute.For<ICaptureRepository>();
        repo.AddAsync(Arg.Any<Capture>(), Arg.Any<CancellationToken>())
            .Returns<Task<Capture>>(_ => throw new InvalidOperationException("db down"));
        var storage = Substitute.For<IAttachmentStorage>();
        storage.SaveAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("2026/05/abc123.pdf");
        var publish = Substitute.For<IPublishEndpoint>();
        var sut = new EfCaptureService(repo, publish, storage);

        using var bytes = new MemoryStream(new byte[1]);
        var input = new AttachmentInput { Content = bytes, FileName = "x.pdf", ContentType = "application/pdf", SizeBytes = 1 };

        await sut.Invoking(s => s.SubmitAsync(null, ChannelKind.Web, input))
            .Should().ThrowAsync<InvalidOperationException>();

        await storage.Received(1).DeleteAsync("2026/05/abc123.pdf", Arg.Any<CancellationToken>());
    }
}

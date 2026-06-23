using FlowHub.Core.Captures;
using FlowHub.Core.Events;
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

    // --- Pin previously-uncovered behaviour (issue #96 ratchet) -------------

    [Fact]
    public async Task SubmitAsync_WithAttachment_PublishesCaptureCreatedWithHasAttachmentTrue()
    {
        var repo = Substitute.For<ICaptureRepository>();
        repo.AddAsync(Arg.Any<Capture>(), Arg.Any<CancellationToken>())
            .Returns(ci => Task.FromResult(ci.Arg<Capture>()));
        var storage = Substitute.For<IAttachmentStorage>();
        storage.SaveAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("2026/05/abc.pdf");
        var publish = Substitute.For<IPublishEndpoint>();
        var sut = new EfCaptureService(repo, publish, storage);

        using var bytes = new MemoryStream(new byte[3]);
        var input = new AttachmentInput { Content = bytes, FileName = "x.pdf", ContentType = "application/pdf", SizeBytes = 3 };

        await sut.SubmitAsync(null, ChannelKind.Api, input);

        await publish.Received(1).Publish(
            Arg.Is<CaptureCreated>(m => m.HasAttachment == true),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SubmitAsync_WithNullAttachment_DelegatesToContentOverload()
    {
        // Verifies the early-return-on-null-attachment branch: the storage is never touched,
        // and the published message has HasAttachment == false.
        var repo = Substitute.For<ICaptureRepository>();
        repo.AddAsync(Arg.Any<Capture>(), Arg.Any<CancellationToken>())
            .Returns(ci => Task.FromResult(ci.Arg<Capture>()));
        var storage = Substitute.For<IAttachmentStorage>();
        var publish = Substitute.For<IPublishEndpoint>();
        var sut = new EfCaptureService(repo, publish, storage);

        await sut.SubmitAsync("plain content", ChannelKind.Web, attachment: null);

        await storage.DidNotReceive().SaveAsync(
            Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await publish.Received(1).Publish(
            Arg.Is<CaptureCreated>(m => m.HasAttachment == false),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SubmitAsync_NullContentAndNullAttachment_ThrowsArgumentNullException()
    {
        var sut = new EfCaptureService(
            Substitute.For<ICaptureRepository>(),
            Substitute.For<IPublishEndpoint>(),
            Substitute.For<IAttachmentStorage>());

        await sut.Invoking(s => s.SubmitAsync(null, ChannelKind.Web, attachment: null))
            .Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task SubmitAsync_WithAttachment_StripsDirectoryPartsFromFileName()
    {
        // FileName goes through Path.GetFileName; an upload that tried to smuggle a path
        // separator must end up using only the basename in BOTH the stored Attachment.FileName
        // and the Capture.Content. Without this, the `Path.GetFileName(attachment.FileName)`
        // mutant (replace with `attachment.FileName`) survives.
        var repo = Substitute.For<ICaptureRepository>();
        repo.AddAsync(Arg.Any<Capture>(), Arg.Any<CancellationToken>())
            .Returns(ci => Task.FromResult(ci.Arg<Capture>()));
        var storage = Substitute.For<IAttachmentStorage>();
        storage.SaveAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("2026/05/abc.pdf");
        var publish = Substitute.For<IPublishEndpoint>();
        var sut = new EfCaptureService(repo, publish, storage);

        using var bytes = new MemoryStream(new byte[1]);
        var input = new AttachmentInput
        {
            Content = bytes,
            FileName = "subdir/innocent.pdf",
            ContentType = "application/pdf",
            SizeBytes = 1,
        };

        var capture = await sut.SubmitAsync(null, ChannelKind.Web, input);

        capture.Content.Should().Be("innocent.pdf");
        capture.Attachment!.FileName.Should().Be("innocent.pdf");
    }
}

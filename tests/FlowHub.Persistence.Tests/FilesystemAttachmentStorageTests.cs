using FlowHub.Core.Captures;
using FlowHub.Persistence;
using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace FlowHub.Persistence.Tests;

public class FilesystemAttachmentStorageTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "flowhub-upload-tests-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task SaveAsync_WritesFileUnderConfiguredRoot_AndReturnsRelativePath()
    {
        var sut = NewSut();
        using var stream = new MemoryStream(new byte[] { 1, 2, 3, 4 });

        var relative = await sut.SaveAsync(stream, "invoice.pdf", "application/pdf");

        relative.Should().MatchRegex(@"^\d{4}/\d{2}/[0-9a-f]{32}\.pdf$");
        var absolute = Path.Combine(_root, relative);
        File.Exists(absolute).Should().BeTrue();
        (await File.ReadAllBytesAsync(absolute)).Should().Equal(1, 2, 3, 4);
    }

    [Fact]
    public async Task DeleteAsync_RemovesFile()
    {
        var sut = NewSut();
        var relative = await sut.SaveAsync(new MemoryStream(new byte[] { 1 }), "x.png", "image/png");

        await sut.DeleteAsync(relative);

        File.Exists(Path.Combine(_root, relative)).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_MissingFile_DoesNotThrow()
    {
        var sut = NewSut();
        await sut.Invoking(s => s.DeleteAsync("2026/01/missing.pdf")).Should().NotThrowAsync();
    }

    private FilesystemAttachmentStorage NewSut()
    {
        var env = Substitute.For<IHostEnvironment>();
        env.ContentRootPath.Returns(_root);
        var opts = Options.Create(new UploadOptions { StoragePath = "", MaxBytes = 2_097_152, AllowedContentTypes = ["application/pdf", "image/png"] });
        return new FilesystemAttachmentStorage(env, opts);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
        GC.SuppressFinalize(this);
    }
}

using FlowHub.Core.Captures;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

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

    [Fact]
    public async Task OpenReadAsync_ReturnsBytesPreviouslySaved()
    {
        var sut = NewSut();
        var bytes = new byte[] { 1, 2, 3, 4 };
        using var input = new MemoryStream(bytes);
        var relative = await sut.SaveAsync(input, "x.pdf", "application/pdf");

        await using var read = await sut.OpenReadAsync(relative);
        using var buffer = new MemoryStream();
        await read.CopyToAsync(buffer);

        buffer.ToArray().Should().Equal(bytes);
    }

    // --- Pin previously-uncovered behaviour (issue #96 ratchet) -------------

    [Fact]
    public async Task OpenReadAsync_WhenCancelled_Throws()
    {
        // Pins the `cancellationToken.ThrowIfCancellationRequested()` call —
        // without this the statement-removal mutant survives.
        var sut = NewSut();
        var relative = await sut.SaveAsync(new MemoryStream(new byte[] { 1 }), "x.pdf", "application/pdf");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => sut.OpenReadAsync(relative, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task SaveAsync_FileNameWithoutExtension_ProducesEmptyExtensionInRelativePath()
    {
        // Pins the `Path.GetExtension(Path.GetFileName(fileName))` chain — the
        // relative path's tail is the bare GUID when no extension is present.
        var sut = NewSut();
        using var stream = new MemoryStream(new byte[] { 9 });

        var relative = await sut.SaveAsync(stream, "no-extension", "application/octet-stream");

        relative.Should().MatchRegex(@"^\d{4}/\d{2}/[0-9a-f]{32}$");
    }

    [Fact]
    public async Task SaveAsync_FileNameWithDirectoryParts_UsesOnlyBaseNameExtension()
    {
        // Pins the `Path.GetFileName(fileName)` part of the chain — a path with
        // separators must still produce a relative path whose extension matches
        // the *basename* (not e.g. an extension on the directory part).
        var sut = NewSut();
        using var stream = new MemoryStream(new byte[] { 1 });

        var relative = await sut.SaveAsync(stream, "dir.with.dots/actual-file.pdf", "application/pdf");

        relative.Should().EndWith(".pdf");
    }

    [Fact]
    public async Task SaveAsync_UsesYearAndMonthFromUtcNow_InRelativePath()
    {
        // Pins the year/month string formatting: the relative path's first two
        // segments must be the current UTC year (4 digits) and month (2 digits).
        var sut = NewSut();
        using var stream = new MemoryStream(new byte[] { 1 });
        var before = DateTimeOffset.UtcNow;

        var relative = await sut.SaveAsync(stream, "x.pdf", "application/pdf");

        var after = DateTimeOffset.UtcNow;
        var segments = relative.Split('/');
        segments.Should().HaveCount(3);
        // The year/month must match either the before or after snapshot (the SUT
        // takes its own UtcNow internally, which sits in [before, after]).
        var actualYear = int.Parse(segments[0], System.Globalization.CultureInfo.InvariantCulture);
        var actualMonth = int.Parse(segments[1], System.Globalization.CultureInfo.InvariantCulture);
        (actualYear, actualMonth).Should().BeOneOf(
            (before.Year, before.Month),
            (after.Year, after.Month));
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

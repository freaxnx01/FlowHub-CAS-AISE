using System.Globalization;
using FlowHub.Core.Captures;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FlowHub.Persistence;

public sealed class FilesystemAttachmentStorage : IAttachmentStorage
{
    private readonly IHostEnvironment _env;
    private readonly IOptions<UploadOptions> _options;

    public FilesystemAttachmentStorage(IHostEnvironment env, IOptions<UploadOptions> options)
    {
        _env = env;
        _options = options;
    }

    public async Task<string> SaveAsync(
        Stream content, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var safeExt = Path.GetExtension(Path.GetFileName(fileName));
        var relative = string.Join('/', now.ToString("yyyy", CultureInfo.InvariantCulture), now.ToString("MM", CultureInfo.InvariantCulture), $"{Guid.NewGuid():N}{safeExt}");
        var absolute = Path.Combine(AbsoluteRoot(), relative);

        Directory.CreateDirectory(Path.GetDirectoryName(absolute)!);
        await using var file = File.Create(absolute);
        await content.CopyToAsync(file, cancellationToken);

        return relative;
    }

    public Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var absolute = Path.Combine(AbsoluteRoot(), relativePath);
        if (File.Exists(absolute))
        {
            File.Delete(absolute);
        }
        return Task.CompletedTask;
    }

    private string AbsoluteRoot() => Path.Combine(_env.ContentRootPath, _options.Value.StoragePath);
}

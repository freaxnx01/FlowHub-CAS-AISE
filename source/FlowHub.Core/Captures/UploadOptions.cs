using System.ComponentModel.DataAnnotations;

namespace FlowHub.Core.Captures;

public sealed class UploadOptions
{
    public const int DefaultMaxBytes = 2 * 1024 * 1024;

    [Required, MinLength(1)]
    public string StoragePath { get; init; } = "App_Data/uploads";

    [Range(1, long.MaxValue)]
    public long MaxBytes { get; init; } = DefaultMaxBytes;

    public IReadOnlyList<string> AllowedContentTypes { get; init; } =
        ["application/pdf", "image/png", "image/jpeg"];
}

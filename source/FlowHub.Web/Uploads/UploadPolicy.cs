using FlowHub.Core.Captures;
using Microsoft.Extensions.Options;

namespace FlowHub.Web.Uploads;

public sealed class UploadPolicy : IUploadPolicy
{
    private readonly IOptionsMonitor<UploadOptions> _options;

    public UploadPolicy(IOptionsMonitor<UploadOptions> options) => _options = options;

    public long MaxBytes => _options.CurrentValue.MaxBytes;
    public IReadOnlyList<string> AllowedContentTypes => _options.CurrentValue.AllowedContentTypes;
    public string AcceptAttribute => string.Join(",", _options.CurrentValue.AllowedContentTypes);
}

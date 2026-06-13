using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FlowHub.Core.Captures;
using FlowHub.Core.Skills;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FlowHub.Skills.Paperless;

public sealed partial class PaperlessSkillIntegration : ISkillIntegration
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly PaperlessOptions _options;
    private readonly IAttachmentStorage _storage;
    private readonly ILogger<PaperlessSkillIntegration> _log;

    public PaperlessSkillIntegration(
        HttpClient http,
        IOptions<PaperlessOptions> options,
        IAttachmentStorage storage,
        ILogger<PaperlessSkillIntegration> log)
    {
        _http = http;
        _options = options.Value;
        _storage = storage;
        _log = log;
    }

    public string Name => "Paperless";

    public async Task<SkillResult> HandleAsync(Capture capture, CancellationToken cancellationToken)
    {
        if (capture.Attachment is null)
        {
            throw new InvalidOperationException(
                $"Capture {capture.Id} routed to Paperless has no attachment.");
        }

        var attachment = capture.Attachment;
        var bytes = await _storage.OpenReadAsync(attachment.RelativePath, cancellationToken);

        var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(attachment.ContentType);
        content.Add(fileContent, "document", attachment.FileName);
        content.Add(new StringContent(capture.Title ?? attachment.FileName), "title");

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/documents/post_document/")
        {
            Content = content,
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Token", _options.ApiToken);

        using var response = await _http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        // post_document returns the async consume task UUID as a quoted JSON string.
        var taskId = await response.Content.ReadFromJsonAsync<string>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Paperless response body was empty.");

        return new SkillResult(Success: true, ExternalRef: taskId);
    }
}

using System.Text;
using FlowHub.Core.Events;
using Microsoft.Extensions.Options;

namespace FlowHub.Web.Notifications;

/// <summary>
/// Publishes a one-line notification per Capture to an ntfy.sh topic. Best-effort:
/// failures are logged and swallowed so a notification outage never breaks capture
/// ingestion. The dynamic content goes in the UTF-8 body; headers stay ASCII-safe.
/// </summary>
public sealed partial class NtfyCaptureNotifier : ICaptureNotifier
{
    private readonly HttpClient _http;
    private readonly DemoNotifyOptions _options;
    private readonly ILogger<NtfyCaptureNotifier> _logger;

    public NtfyCaptureNotifier(HttpClient http, IOptions<DemoNotifyOptions> options, ILogger<NtfyCaptureNotifier> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task NotifyCaptureCreatedAsync(CaptureCreated capture, CancellationToken cancellationToken)
    {
        var body = $"{capture.Source}: {Snippet(capture.Content, 240)}";
        using var request = new HttpRequestMessage(HttpMethod.Post, _options.Topic)
        {
            Content = new StringContent(body, Encoding.UTF8, "text/plain"),
        };
        request.Headers.TryAddWithoutValidation("Title", "New FlowHub demo capture");
        request.Headers.TryAddWithoutValidation("Tags", "inbox_tray");
        if (!string.IsNullOrWhiteSpace(_options.AccessToken))
            request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {_options.AccessToken}");

        try
        {
            using var response = await _http.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                LogFailed(capture.CaptureId, (int)response.StatusCode);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogError(capture.CaptureId, ex.Message);
        }
    }

    private static string Snippet(string content, int max)
    {
        var s = content.ReplaceLineEndings(" ").Trim();
        return s.Length <= max ? s : s[..max] + "…";
    }

    [LoggerMessage(EventId = 4001, Level = LogLevel.Warning,
        Message = "ntfy notify for capture {CaptureId} returned HTTP {StatusCode}")]
    private partial void LogFailed(Guid captureId, int statusCode);

    [LoggerMessage(EventId = 4002, Level = LogLevel.Warning,
        Message = "ntfy notify for capture {CaptureId} failed: {Reason}")]
    private partial void LogError(Guid captureId, string reason);
}

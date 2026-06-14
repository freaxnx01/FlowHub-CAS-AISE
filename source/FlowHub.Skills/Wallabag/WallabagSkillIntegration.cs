using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using FlowHub.Core.Captures;
using FlowHub.Core.Skills;
using Microsoft.Extensions.Logging;
// Disambiguate the domain Capture from System.Text.RegularExpressions.Capture.
using Capture = FlowHub.Core.Captures.Capture;

namespace FlowHub.Skills.Wallabag;

public sealed partial class WallabagSkillIntegration : ISkillIntegration
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly WallabagTokenProvider _tokenProvider;
    private readonly ILogger<WallabagSkillIntegration> _log;

    public WallabagSkillIntegration(
        HttpClient http,
        WallabagTokenProvider tokenProvider,
        ILogger<WallabagSkillIntegration> log)
    {
        _http = http;
        _tokenProvider = tokenProvider;
        _log = log;
    }

    public string Name => "Wallabag";

    public async Task<SkillResult> HandleAsync(Capture capture, CancellationToken cancellationToken)
    {
        if (!TryExtractUrl(capture.Content, out var uri))
        {
            throw new InvalidOperationException(
                $"Capture {capture.Id} content contains no http(s) url to save: '{capture.Content}'");
        }

        var token = await _tokenProvider.GetTokenAsync(cancellationToken);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/entries.json")
        {
            Content = JsonContent.Create(new { url = uri.ToString() }, options: JsonOptions),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await _http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<WallabagEntryResponse>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Wallabag response body was empty.");

        if (payload.Id is null)
        {
            throw new InvalidOperationException("Wallabag response did not include an 'id' field.");
        }

        return new SkillResult(Success: true, ExternalRef: payload.Id.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Extracts the first absolute http(s) URL contained in the capture content.
    /// A read-later capture often arrives as free text (e.g. "save &lt;url&gt; to read
    /// later"), so the URL must be located within the content rather than assuming
    /// the whole content is a URL. Trailing sentence punctuation is trimmed.
    /// </summary>
    private static bool TryExtractUrl(string content, [NotNullWhen(true)] out Uri? uri)
    {
        uri = null;
        if (string.IsNullOrWhiteSpace(content))
        {
            return false;
        }

        foreach (Match match in UrlRegex().Matches(content))
        {
            var candidate = match.Value.TrimEnd('.', ',', ';', ':', '!', '?', ')', ']', '}', '"', '\'', '>');
            if (Uri.TryCreate(candidate, UriKind.Absolute, out var parsed)
                && (parsed.Scheme == Uri.UriSchemeHttp || parsed.Scheme == Uri.UriSchemeHttps))
            {
                uri = parsed;
                return true;
            }
        }

        return false;
    }

    [GeneratedRegex(@"https?://\S+", RegexOptions.IgnoreCase)]
    private static partial Regex UrlRegex();

    private sealed record WallabagEntryResponse(long? Id);
}

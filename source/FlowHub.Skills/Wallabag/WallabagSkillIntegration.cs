using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FlowHub.Core.Captures;
using FlowHub.Core.Skills;
using Microsoft.Extensions.Logging;

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
        if (!Uri.TryCreate(capture.Content, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException(
                $"Capture {capture.Id} content is not a valid absolute url: '{capture.Content}'");
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

    private sealed record WallabagEntryResponse(long? Id);
}

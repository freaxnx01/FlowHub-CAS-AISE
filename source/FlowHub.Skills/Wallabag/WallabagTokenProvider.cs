using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FlowHub.Skills.Wallabag;

/// <summary>
/// Mints and caches a Wallabag OAuth2 access token (password grant) and refreshes it
/// shortly before expiry, so the skill can run unattended through a long-lived demo.
/// Mirrors <see cref="Vikunja.VikunjaProjectCatalog"/>'s single-flight + TimeProvider
/// caching: a fast lock-free reuse path plus a double-checked refresh under a gate.
/// </summary>
public sealed partial class WallabagTokenProvider : IDisposable
{
    private static readonly TimeSpan ExpiryMargin = TimeSpan.FromSeconds(60);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly WallabagOptions _options;
    private readonly TimeProvider _time;
    private readonly ILogger<WallabagTokenProvider> _log;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private string? _token;
    private DateTimeOffset _expiresAt;

    public WallabagTokenProvider(
        HttpClient http,
        IOptions<WallabagOptions> options,
        TimeProvider time,
        ILogger<WallabagTokenProvider> log)
    {
        _http = http;
        _options = options.Value;
        _time = time;
        _log = log;
    }

    public async Task<string> GetTokenAsync(CancellationToken cancellationToken)
    {
        var now = _time.GetUtcNow();
        if (_token is not null && now < _expiresAt - ExpiryMargin)
        {
            return _token;
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            // Re-read time inside the lock: a waiter that blocked through another
            // thread's successful refresh must see the fresh token + expiry.
            now = _time.GetUtcNow();
            if (_token is not null && now < _expiresAt - ExpiryMargin)
            {
                return _token;
            }

            return await FetchTokenAsync(now, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<string> FetchTokenAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/oauth/v2/token")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "password",
                    ["client_id"] = _options.ClientId ?? string.Empty,
                    ["client_secret"] = _options.ClientSecret ?? string.Empty,
                    ["username"] = _options.Username ?? string.Empty,
                    ["password"] = _options.Password ?? string.Empty,
                }),
            };

            using var response = await _http.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var grant = await response.Content.ReadFromJsonAsync<TokenGrant>(JsonOptions, cancellationToken)
                ?? throw new InvalidOperationException("Wallabag token response body was empty.");

            if (string.IsNullOrWhiteSpace(grant.AccessToken))
            {
                throw new InvalidOperationException("Wallabag token response did not include an 'access_token'.");
            }

            _token = grant.AccessToken;
            _expiresAt = now + TimeSpan.FromSeconds(grant.ExpiresIn);
            return _token;
        }
        catch (Exception ex)
        {
            LogTokenRefreshFailed(ex.GetType().Name);
            throw;
        }
    }

    public void Dispose() => _gate.Dispose();

    [LoggerMessage(EventId = 3040, Level = LogLevel.Warning,
        Message = "Wallabag token refresh failed (reason={Reason})")]
    private partial void LogTokenRefreshFailed(string reason);

    private sealed record TokenGrant(
        [property: JsonPropertyName("access_token")] string? AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);
}

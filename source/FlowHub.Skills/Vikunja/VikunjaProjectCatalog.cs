using System.Net.Http.Headers;
using System.Net.Http.Json;
using FlowHub.Core.Skills;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FlowHub.Skills.Vikunja;

public sealed partial class VikunjaProjectCatalog : IVikunjaProjectCatalog, IDisposable
{
    private readonly HttpClient _http;
    private readonly VikunjaOptions _options;
    private readonly ILogger<VikunjaProjectCatalog> _log;
    private readonly TimeProvider _time;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private IReadOnlyDictionary<string, int>? _cache;
    private DateTimeOffset _fetchedAt;

    public VikunjaProjectCatalog(
        HttpClient http,
        IOptions<VikunjaOptions> options,
        ILogger<VikunjaProjectCatalog> log,
        TimeProvider time)
    {
        _http = http;
        _options = options.Value;
        _log = log;
        _time = time;
    }

    public async Task<IReadOnlyDictionary<string, int>> GetAsync(CancellationToken cancellationToken)
    {
        var now = _time.GetUtcNow();
        if (_cache is not null && now - _fetchedAt < _options.Catalog.RefreshInterval)
        {
            return _cache;
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            // Re-read time inside the lock: a waiter that blocked through another
            // thread's successful refresh must see the fresh _fetchedAt.
            now = _time.GetUtcNow();
            if (_cache is not null && now - _fetchedAt < _options.Catalog.RefreshInterval)
            {
                return _cache;
            }

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/projects");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiToken);
                using var response = await _http.SendAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();
                var projects = await response.Content.ReadFromJsonAsync<VikunjaProjectDto[]>(cancellationToken)
                    ?? Array.Empty<VikunjaProjectDto>();

                var map = projects
                    .Where(p => !string.IsNullOrWhiteSpace(p.Title))
                    .GroupBy(p => p.Title!, StringComparer.Ordinal)
                    .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.Ordinal);

                _cache = map;
                _fetchedAt = now;
                return _cache;
            }
            catch (Exception ex)
            {
                if (_cache is not null)
                {
                    LogRefreshFailedKeepingCache(ex.GetType().Name);
                    return _cache;
                }

                LogFirstFetchFailed(ex.GetType().Name);
                var fallback = new Dictionary<string, int>(StringComparer.Ordinal)
                {
                    [_options.FallbackProject] = _options.FallbackProjectId,
                };
                _cache = fallback;
                _fetchedAt = now;
                return fallback;
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose() => _gate.Dispose();

    private sealed record VikunjaProjectDto(int Id, string? Title);

    [LoggerMessage(EventId = 3020, Level = LogLevel.Warning,
        Message = "Vikunja catalog first fetch failed (reason={Reason}); using fallback bucket only")]
    private partial void LogFirstFetchFailed(string reason);

    [LoggerMessage(EventId = 3021, Level = LogLevel.Warning,
        Message = "Vikunja catalog refresh failed (reason={Reason}); keeping last-known catalog")]
    private partial void LogRefreshFailedKeepingCache(string reason);
}

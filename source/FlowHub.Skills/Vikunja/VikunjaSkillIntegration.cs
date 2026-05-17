using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using FlowHub.Core.Captures;
using FlowHub.Core.Skills;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FlowHub.Skills.Vikunja;

public sealed partial class VikunjaSkillIntegration : ISkillIntegration
{
    private const int FallbackTitleMaxLength = 120;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly VikunjaOptions _options;
    private readonly IVikunjaProjectCatalog _catalog;
    private readonly ILogger<VikunjaSkillIntegration> _log;

    public VikunjaSkillIntegration(
        HttpClient http,
        IOptions<VikunjaOptions> options,
        IVikunjaProjectCatalog catalog,
        ILogger<VikunjaSkillIntegration> log)
    {
        _http = http;
        _options = options.Value;
        _catalog = catalog;
        _log = log;
    }

    public string Name => "Vikunja";

    public async Task<SkillResult> HandleAsync(Capture capture, CancellationToken cancellationToken)
    {
        var title = !string.IsNullOrWhiteSpace(capture.Title)
            ? capture.Title.Trim()
            : Truncate(capture.Content.Trim(), FallbackTitleMaxLength);

        var projectId = await ResolveProjectIdAsync(capture.VikunjaProject, cancellationToken);

        var path = string.Format(
            CultureInfo.InvariantCulture,
            "/api/v1/projects/{0}/tasks",
            projectId);

        var description = capture.EnrichmentDescription;
        using var request = new HttpRequestMessage(HttpMethod.Put, path)
        {
            Content = JsonContent.Create(
                description is null ? (object)new { title } : new { title, description },
                options: JsonOptions),
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.ApiToken);

        using var response = await _http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<VikunjaTaskResponse>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Vikunja response body was empty.");

        if (payload.Id is null)
        {
            throw new InvalidOperationException("Vikunja response did not include an 'id' field.");
        }

        return new SkillResult(Success: true, ExternalRef: payload.Id.Value.ToString(CultureInfo.InvariantCulture));
    }

    private async Task<int> ResolveProjectIdAsync(string? projectName, CancellationToken cancellationToken)
    {
        var map = await _catalog.GetAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(projectName) && map.TryGetValue(projectName, out var id))
        {
            return id;
        }

        if (map.TryGetValue(_options.FallbackProject, out var fallbackId))
        {
            return fallbackId;
        }

        return _options.FallbackProjectId;
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max];

    private sealed record VikunjaTaskResponse(long? Id);
}

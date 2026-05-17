using FlowHub.Core.Captures;
using FlowHub.Core.Classification;
using FlowHub.Core.Skills;
using Microsoft.Extensions.Logging;

namespace FlowHub.AI;

/// <summary>
/// Fallback bucket reference resolved from <c>Skills:Vikunja</c>. Defined here
/// (not in FlowHub.Skills) to keep AI module independent of skills internals.
/// </summary>
public sealed record VikunjaFallback(string Name, int Id);

public sealed partial class EnricherDispatcher
{
    private readonly Dictionary<string, IEnricher> _enrichers;
    private readonly IVikunjaProjectCatalog _catalog;
    private readonly VikunjaFallback _fallback;
    private readonly ILogger<EnricherDispatcher> _log;

    public EnricherDispatcher(
        IEnumerable<IEnricher> enrichers,
        IVikunjaProjectCatalog catalog,
        VikunjaFallback fallback,
        ILogger<EnricherDispatcher> log)
    {
        _enrichers = enrichers
            .GroupBy(e => e.BucketName, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
        _catalog = catalog;
        _fallback = fallback;
        _log = log;
    }

    public async Task<(string? Project, EnrichmentResult? Enrichment)> DispatchAsync(
        Capture capture,
        ClassificationResult classification,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(classification.MatchedSkill, "Vikunja", StringComparison.Ordinal))
        {
            return (null, null);
        }

        var requested = classification.VikunjaProject;
        var catalog = await _catalog.GetAsync(cancellationToken);

        string project;
        if (string.IsNullOrWhiteSpace(requested) || !catalog.ContainsKey(requested))
        {
            if (!string.IsNullOrWhiteSpace(requested))
            {
                LogProjectCoerced(requested, _fallback.Name);
            }
            project = _fallback.Name;
        }
        else
        {
            project = requested;
        }

        if (!_enrichers.TryGetValue(project, out var enricher))
        {
            return (project, null);
        }

        try
        {
            var result = await enricher.EnrichAsync(capture, classification, cancellationToken);
            return (project, result);
        }
        catch (Exception ex)
        {
            LogEnrichmentFailed(project, ex.GetType().Name);
            return (project, null);
        }
    }

    [LoggerMessage(EventId = 3011, Level = LogLevel.Warning,
        Message = "Classifier returned unknown Vikunja project '{Requested}' — coercing to '{Fallback}'")]
    private partial void LogProjectCoerced(string requested, string fallback);

    [LoggerMessage(EventId = 3030, Level = LogLevel.Warning,
        Message = "Enricher for bucket '{Bucket}' failed (reason={Reason}); posting without enrichment")]
    private partial void LogEnrichmentFailed(string bucket, string reason);
}

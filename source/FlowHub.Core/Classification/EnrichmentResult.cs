namespace FlowHub.Core.Classification;

/// <summary>
/// Output of <see cref="IEnricher.EnrichAsync"/>. A bucket-specific enricher returns
/// a ready-to-use description (markdown), plus any structured fields the bucket cares
/// about (currently unused but kept for future Vikunja custom-field mapping).
/// </summary>
public sealed record EnrichmentResult(
    string Description,
    IReadOnlyDictionary<string, string>? Fields = null);

namespace FlowHub.Core.Skills;

/// <summary>
/// Driving port that exposes the set of Vikunja projects available as routing
/// targets ("buckets"). The classifier prompt is built from this list and the
/// skill integration uses it to resolve a bucket name to a project id.
/// </summary>
public interface IVikunjaProjectCatalog
{
    /// <summary>
    /// Returns a name → projectId map. Implementations are expected to cache and
    /// degrade gracefully on transient API failures; callers should not retry.
    /// </summary>
    Task<IReadOnlyDictionary<string, int>> GetAsync(CancellationToken cancellationToken);
}

namespace FlowHub.Core.Captures;

/// <summary>
/// Aggregated counts of captures needing operator attention.
/// Drives the Dashboard's "Needs Attention" widget.
/// </summary>
public sealed record FailureCounts(int OrphanCount, int UnhandledCount)
{
    public bool AnyFailures => OrphanCount > 0 || UnhandledCount > 0;
}

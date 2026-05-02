namespace FlowHub.Core.Classification;

/// <summary>
/// Driving port for capture classification.
/// Slice B ships <see cref="KeywordClassifier"/>; Slice C swaps in an AI-backed adapter.
/// </summary>
public interface IClassifier
{
    Task<ClassificationResult> ClassifyAsync(string content, CancellationToken cancellationToken);
}

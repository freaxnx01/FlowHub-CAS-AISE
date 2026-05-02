namespace FlowHub.Core.Classification;

/// <summary>
/// Deterministic keyword-based classifier used in Block 3 Slice B.
/// Slice C replaces this with an AI-backed implementation that consumes <see cref="IClassifier"/>.
/// </summary>
public sealed class KeywordClassifier : IClassifier
{
    public Task<ClassificationResult> ClassifyAsync(string content, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (LooksLikeUrl(content))
        {
            return Task.FromResult(new ClassificationResult(["link"], "Wallabag"));
        }

        if (ContainsTodoKeyword(content))
        {
            return Task.FromResult(new ClassificationResult(["task"], "Vikunja"));
        }

        return Task.FromResult(new ClassificationResult(["unsorted"], string.Empty));
    }

    private static bool LooksLikeUrl(string content) =>
        Uri.TryCreate(content.Trim(), UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    private static bool ContainsTodoKeyword(string content) =>
        content.Contains("todo", StringComparison.OrdinalIgnoreCase)
        || content.Contains("task", StringComparison.OrdinalIgnoreCase);
}

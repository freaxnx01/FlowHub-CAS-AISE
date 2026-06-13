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
        var sw = System.Diagnostics.Stopwatch.StartNew();

        var result =
            LooksLikeUrl(content) ? new ClassificationResult(["link"], "Wallabag")
            : ContainsTodoKeyword(content) ? new ClassificationResult(["task"], "Vikunja")
            : new ClassificationResult(["unsorted"], string.Empty);

        sw.Stop();
        var traced = result with
        {
            Trace = new ClassifierTrace(ClassifierKind.Keyword, (int)sw.ElapsedMilliseconds),
        };
        return Task.FromResult(traced);
    }

    private static bool LooksLikeUrl(string content) =>
        Uri.TryCreate(content.Trim(), UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    private static bool ContainsTodoKeyword(string content) =>
        content.Contains("todo", StringComparison.OrdinalIgnoreCase)
        || content.Contains("task", StringComparison.OrdinalIgnoreCase);
}

using System.Diagnostics;
using FlowHub.Core.Classification;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace FlowHub.AI;

internal sealed partial class AiClassifier : IClassifier
{
    private static readonly string[] AllowedSkills = ["Wallabag", "Vikunja", ""];

    private readonly IChatClient _chat;
    private readonly IClassifier _keyword;
    private readonly ILogger<AiClassifier> _log;
    private readonly ChatOptions _options;

    public AiClassifier(
        IChatClient chat,
        IClassifier keyword,
        ILogger<AiClassifier> log,
        ChatOptions options)
    {
        _chat = chat;
        _keyword = keyword;
        _log = log;
        _options = options;
    }

    public async Task<ClassificationResult> ClassifyAsync(string content, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);
        var sw = Stopwatch.StartNew();

        try
        {
            var response = await _chat.GetResponseAsync<AiClassificationResponse>(
                AiPrompts.BuildMessages(content),
                _options,
                cancellationToken: cancellationToken);

            if (!response.TryGetResult(out var payload))
            {
                throw new InvalidOperationException("schema_violation");
            }

            if (Array.IndexOf(AllowedSkills, payload.MatchedSkill) < 0)
            {
                throw new InvalidOperationException("schema_violation");
            }

            return new ClassificationResult(payload.Tags, payload.MatchedSkill, payload.Title);
        }
        catch (Exception ex)
        {
            sw.Stop();
            var reason = ex is InvalidOperationException && ex.Message == "schema_violation"
                ? "schema_violation"
                : ex.GetType().Name;
            LogFellBack(reason, sw.ElapsedMilliseconds);
            return await _keyword.ClassifyAsync(content, cancellationToken);
        }
    }

    [LoggerMessage(
        EventId = 3010,
        Level = LogLevel.Warning,
        Message = "AiClassifier fell back to keyword classifier (reason={Reason}, duration_ms={DurationMs})")]
    private partial void LogFellBack(string reason, long durationMs);
}

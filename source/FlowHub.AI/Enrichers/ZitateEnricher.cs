using System.Text;
using FlowHub.Core.Captures;
using FlowHub.Core.Classification;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace FlowHub.AI.Enrichers;

public sealed partial class ZitateEnricher : IEnricher
{
    private readonly IChatClient _chat;
    private readonly ILogger<ZitateEnricher> _log;

    public ZitateEnricher(IChatClient chat, ILogger<ZitateEnricher> log)
    {
        _chat = chat;
        _log = log;
    }

    public string BucketName => "Zitate";

    private const int MaxAuthorLength = 120;

    public async Task<EnrichmentResult?> EnrichAsync(
        Capture capture,
        ClassificationResult classification,
        CancellationToken cancellationToken)
    {
        var quote = classification.Entities?.GetValueOrDefault("quote") ?? capture.Content.Trim();
        var author = classification.Entities?.GetValueOrDefault("author");

        // Author comes from classifier-extracted entities (model-generated from
        // arbitrary capture content). Cap length to bound the prompt-injection
        // blast radius on the downstream bio-fetch LLM call.
        if (author is { Length: > MaxAuthorLength })
        {
            author = author[..MaxAuthorLength];
        }

        var description = new StringBuilder();
        description.Append('"').Append(quote.Trim('"', ' ', '\n')).Append('"');
        if (!string.IsNullOrWhiteSpace(author))
        {
            description.Append(" — ").Append(author);
        }
        description.AppendLine().AppendLine();

        if (!string.IsNullOrWhiteSpace(author))
        {
            var bio = await FetchBioAsync(author!, quote, cancellationToken);
            if (!string.IsNullOrWhiteSpace(bio))
            {
                description.Append("About ").Append(author).Append(": ").Append(bio.Trim());
            }
        }

        return new EnrichmentResult(description.ToString().TrimEnd());
    }

    private async Task<string?> FetchBioAsync(string author, string quote, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _chat.GetResponseAsync(
                ZitateEnricherPrompts.BuildMessages(author, quote),
                new ChatOptions { MaxOutputTokens = 280, Temperature = 0.2f },
                cancellationToken);
            return response.Messages.LastOrDefault()?.Text;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            LogBioFetchFailed(author, ex.GetType().Name);
            return null;
        }
    }

    [LoggerMessage(EventId = 3031, Level = LogLevel.Warning,
        Message = "ZitateEnricher bio fetch failed for author='{Author}' (reason={Reason})")]
    private partial void LogBioFetchFailed(string author, string reason);
}

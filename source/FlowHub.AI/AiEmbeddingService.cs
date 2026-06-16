using FlowHub.Core.Captures;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace FlowHub.AI;

/// <summary>
/// LLM-backed <see cref="IEmbeddingService"/> (ADR 0006): turns capture text into a
/// vector embedding for pgvector semantic search. Provider errors are caught and
/// treated as best-effort — the capture is stored without an embedding and search
/// degrades to the non-vector path rather than failing the pipeline.
/// </summary>
public sealed partial class AiEmbeddingService : IEmbeddingService
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _generator;
    private readonly ILogger<AiEmbeddingService> _log;
    private readonly EmbeddingGenerationOptions? _options;

    public AiEmbeddingService(
        IEmbeddingGenerator<string, Embedding<float>> generator,
        ILogger<AiEmbeddingService> log,
        int? dimensions = null)
    {
        _generator = generator;
        _log = log;
        _options = dimensions is { } dim ? new EmbeddingGenerationOptions { Dimensions = dim } : null;
    }

    public async Task<float[]?> GenerateAsync(string text, CancellationToken cancellationToken = default)
    {
        // TEMP DEBUG (umlaut investigation) — logs the exact bytes every caller passes.
        LogEmbedDebug(text.Length, Convert.ToHexString(System.Text.Encoding.UTF8.GetBytes(text)), text);
        try
        {
            var result = await _generator.GenerateAsync([text], _options, cancellationToken);
            return result[0].Vector.ToArray();
        }
        catch (Exception ex)
        {
            LogEmbeddingFailed(ex);
            return null;
        }
    }

    [LoggerMessage(EventId = 6001, Level = LogLevel.Warning,
        Message = "Embedding generation failed — Capture will be stored without embedding.")]
    private partial void LogEmbeddingFailed(Exception ex);

    [LoggerMessage(EventId = 6099, Level = LogLevel.Warning,
        Message = "EMBED-DEBUG len={Len} hex={Hex} text={Text}")]
    private partial void LogEmbedDebug(int len, string hex, string text);
}

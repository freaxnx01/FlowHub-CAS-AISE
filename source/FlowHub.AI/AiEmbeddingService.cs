using FlowHub.Core.Captures;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace FlowHub.AI;

public sealed partial class AiEmbeddingService : IEmbeddingService
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _generator;
    private readonly ILogger<AiEmbeddingService> _log;

    public AiEmbeddingService(
        IEmbeddingGenerator<string, Embedding<float>> generator,
        ILogger<AiEmbeddingService> log)
    {
        _generator = generator;
        _log = log;
    }

    public async Task<float[]?> GenerateAsync(string text, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _generator.GenerateAsync([text], cancellationToken: cancellationToken);
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
}

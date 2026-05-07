using FlowHub.Core.Captures;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace FlowHub.Api.Endpoints;

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/admin")
            .RequireAuthorization()
            .WithTags("Admin");

        group.MapPost("/embeddings/rebuild", RebuildEmbeddingsAsync)
            .WithName("RebuildEmbeddings")
            .Produces<RebuildResult>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        return app;
    }

    private sealed record RebuildResult(int Processed, int Skipped, int Failed);

    private static async Task<Results<Ok<RebuildResult>, ProblemHttpResult>> RebuildEmbeddingsAsync(
        IEmbeddingService embeddingService,
        ICaptureRepository captureRepository,
        ICaptureService captureService,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var probe = await embeddingService.GenerateAsync("probe", ct);
        if (probe is null)
        {
            return TypedResults.Problem(
                type: ProblemTypes.Validation,
                title: "Embedding service not configured.",
                detail: "Set Embeddings__ApiKey before running a rebuild.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                instance: httpContext.Request.Path);
        }

        var ids = await captureRepository.GetIdsWithoutEmbeddingAsync(ct);
        int processed = 0, skipped = 0, failed = 0;

        foreach (var id in ids)
        {
            var capture = await captureService.GetByIdAsync(id, ct);
            if (capture is null) { skipped++; continue; }

            var embedding = await embeddingService.GenerateAsync(capture.Content, ct);
            if (embedding is null) { failed++; continue; }

            await captureRepository.StoreEmbeddingAsync(id, embedding, ct);
            processed++;
        }

        return TypedResults.Ok(new RebuildResult(processed, skipped, failed));
    }
}

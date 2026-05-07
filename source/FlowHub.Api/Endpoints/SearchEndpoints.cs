using FlowHub.Core.Captures;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace FlowHub.Api.Endpoints;

public static class SearchEndpoints
{
    public static IEndpointRouteBuilder MapSearchEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/captures")
            .RequireAuthorization()
            .WithTags("Captures");

        group.MapGet("/search", SearchAsync)
            .WithName("SearchCaptures")
            .Produces<IReadOnlyList<Capture>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        return app;
    }

    private static async Task<Results<Ok<IReadOnlyList<Capture>>, ProblemHttpResult>> SearchAsync(
        string q,
        IEmbeddingService embeddingService,
        ICaptureRepository captureRepository,
        HttpContext httpContext,
        int? limit,
        CancellationToken ct)
    {
        var queryEmbedding = await embeddingService.GenerateAsync(q, ct);
        if (queryEmbedding is null)
        {
            return TypedResults.Problem(
                type: ProblemTypes.Validation,
                title: "Semantic search not available.",
                detail: "The embedding service is not configured. Set Embeddings__ApiKey to enable semantic search.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                instance: httpContext.Request.Path);
        }

        var results = await captureRepository.SearchByEmbeddingAsync(queryEmbedding, limit ?? 10, ct);
        return TypedResults.Ok(results);
    }
}

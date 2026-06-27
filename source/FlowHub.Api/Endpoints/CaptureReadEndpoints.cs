using FlowHub.Core.Captures;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace FlowHub.Api.Endpoints;

internal static class CaptureReadEndpoints
{
    public sealed record ListCapturesResponse(IReadOnlyList<Capture> Items, string? NextCursor);

    public static void MapCaptureReadEndpoints(this RouteGroupBuilder captures)
    {
        captures.MapGet("/", ListAsync)
            .WithName("ListCaptures")
            .Produces<ListCapturesResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        captures.MapGet("/{id:guid}", GetByIdAsync)
            .WithName("GetCapture")
            .Produces<Capture>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<Results<Ok<ListCapturesResponse>, ProblemHttpResult>> ListAsync(
        ICaptureService captureService,
        HttpContext httpContext,
        string? stage,
        ChannelKind? source,
        int? limit,
        string? cursor,
        CancellationToken ct)
    {
        IReadOnlyList<LifecycleStage>? stages = null;
        if (!string.IsNullOrEmpty(stage))
        {
            try
            {
                stages = stage.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(s => Enum.Parse<LifecycleStage>(s, ignoreCase: true))
                    .ToArray();
            }
            catch (ArgumentException)
            {
                return TypedResults.Problem(
                    type: ProblemTypes.Validation,
                    title: "Invalid stage filter.",
                    detail: $"'{stage}' contains an unknown LifecycleStage.",
                    statusCode: StatusCodes.Status400BadRequest,
                    instance: httpContext.Request.Path);
            }
        }

        CaptureCursor? captureCursor = null;
        if (!string.IsNullOrEmpty(cursor))
        {
            try
            {
                captureCursor = CaptureCursor.Decode(cursor);
            }
            catch (FormatException)
            {
                return TypedResults.Problem(
                    type: ProblemTypes.Validation,
                    title: "Invalid pagination cursor.",
                    detail: "The cursor is not a valid Base64Url-encoded value.",
                    statusCode: StatusCodes.Status400BadRequest,
                    instance: httpContext.Request.Path);
            }
        }

        var filter = new CaptureFilter(stages, source, limit ?? 50, captureCursor);
        var page = await captureService.ListAsync(filter, ct);

        return TypedResults.Ok(new ListCapturesResponse(page.Items, page.Next?.Encode()));
    }

    private static async Task<Results<Ok<Capture>, ProblemHttpResult>> GetByIdAsync(
        Guid id,
        ICaptureService captureService,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var capture = await captureService.GetByIdAsync(id, ct);
        if (capture is null)
        {
            return TypedResults.Problem(
                type: ProblemTypes.CaptureNotFound,
                title: "Capture not found.",
                detail: $"No capture exists with id {id}.",
                statusCode: StatusCodes.Status404NotFound,
                instance: httpContext.Request.Path);
        }
        return TypedResults.Ok(capture);
    }
}

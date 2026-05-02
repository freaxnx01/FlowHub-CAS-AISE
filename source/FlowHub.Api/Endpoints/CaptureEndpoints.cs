using FluentValidation;
using FlowHub.Api.Requests;
using FlowHub.Core.Captures;
using FlowHub.Core.Events;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace FlowHub.Api.Endpoints;

public static class CaptureEndpoints
{
    public static IEndpointRouteBuilder MapFlowHubApi(this IEndpointRouteBuilder app)
    {
        var captures = app.MapGroup("/api/v1/captures")
            .RequireAuthorization()
            .WithTags("Captures");

        captures.MapPost("/", SubmitAsync)
            .WithName("SubmitCapture")
            .Produces<Capture>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        captures.MapGet("/", ListAsync)
            .WithName("ListCaptures")
            .Produces<ListCapturesResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        captures.MapGet("/{id:guid}", GetByIdAsync)
            .WithName("GetCapture")
            .Produces<Capture>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        captures.MapPost("/{id:guid}/retry", RetryAsync)
            .WithName("RetryCapture")
            .Produces<Capture>(StatusCodes.Status202Accepted)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    public sealed record ListCapturesResponse(IReadOnlyList<Capture> Items, string? NextCursor);

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
                    type: "https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/problems/validation.md",
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
                    type: "https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/problems/validation.md",
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
                type: "https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/problems/capture-not-found.md",
                title: "Capture not found.",
                detail: $"No capture exists with id {id}.",
                statusCode: StatusCodes.Status404NotFound,
                instance: httpContext.Request.Path);
        }
        return TypedResults.Ok(capture);
    }

    private static readonly LifecycleStage[] RetryableStages = [LifecycleStage.Orphan, LifecycleStage.Unhandled];

    private static async Task<Results<Accepted<Capture>, ProblemHttpResult>> RetryAsync(
        Guid id,
        ICaptureService captureService,
        IBus bus,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var capture = await captureService.GetByIdAsync(id, ct);
        if (capture is null)
        {
            return TypedResults.Problem(
                type: "https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/problems/capture-not-found.md",
                title: "Capture not found.",
                detail: $"No capture exists with id {id}.",
                statusCode: StatusCodes.Status404NotFound,
                instance: httpContext.Request.Path);
        }

        if (!RetryableStages.Contains(capture.Stage))
        {
            return TypedResults.Problem(
                type: "https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/problems/capture-not-retryable.md",
                title: "Capture stage is not retryable.",
                detail: $"Captures may only be retried from Orphan or Unhandled. Current stage: {capture.Stage}.",
                statusCode: StatusCodes.Status409Conflict,
                instance: httpContext.Request.Path);
        }

        await captureService.ResetForRetryAsync(id, ct);

        // Build the reset record directly rather than re-querying — avoids a race where the
        // in-memory MassTransit consumer has already classified the capture before we read it back.
        var reset = capture with { Stage = LifecycleStage.Raw, FailureReason = null };

        await bus.Publish(new CaptureCreated(capture.Id, capture.Content, capture.Source, capture.CreatedAt), ct);

        return TypedResults.Accepted($"/api/v1/captures/{id}", reset);
    }

    private static async Task<Results<Created<Capture>, ValidationProblem>> SubmitAsync(
        CreateCaptureRequest request,
        IValidator<CreateCaptureRequest> validator,
        ICaptureService captureService,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            var errors = validation.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            return TypedResults.ValidationProblem(errors);
        }

        var capture = await captureService.SubmitAsync(request.Content, request.Source, ct);
        return TypedResults.Created($"/api/v1/captures/{capture.Id}", capture);
    }
}

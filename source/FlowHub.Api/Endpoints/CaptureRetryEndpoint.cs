using FlowHub.Core.Captures;
using FlowHub.Core.Events;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace FlowHub.Api.Endpoints;

internal static class CaptureRetryEndpoint
{
    private static readonly LifecycleStage[] RetryableStages = [LifecycleStage.Orphan, LifecycleStage.Unhandled];

    public static void MapCaptureRetryEndpoint(this RouteGroupBuilder captures)
    {
        captures.MapPost("/{id:guid}/retry", RetryAsync)
            .WithName("RetryCapture")
            .Produces<Capture>(StatusCodes.Status202Accepted)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

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
                type: ProblemTypes.CaptureNotFound,
                title: "Capture not found.",
                detail: $"No capture exists with id {id}.",
                statusCode: StatusCodes.Status404NotFound,
                instance: httpContext.Request.Path);
        }

        if (!RetryableStages.Contains(capture.Stage))
        {
            return TypedResults.Problem(
                type: ProblemTypes.CaptureNotRetryable,
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
}

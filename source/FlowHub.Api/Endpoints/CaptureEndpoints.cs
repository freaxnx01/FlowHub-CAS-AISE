using FluentValidation;
using FlowHub.Api.Requests;
using FlowHub.Core.Captures;
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

        return app;
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

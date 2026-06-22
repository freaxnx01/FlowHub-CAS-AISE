using FluentValidation;
using FlowHub.Api.Requests;
using FlowHub.Core.Captures;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace FlowHub.Api.Endpoints;

internal static class CaptureWriteEndpoints
{
    public static void MapCaptureWriteEndpoints(this RouteGroupBuilder captures)
    {
        captures.MapPost("/", SubmitAsync)
            .WithName("SubmitCapture")
            .Produces<Capture>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        captures.MapPost("/upload", UploadAsync)
            .WithName("UploadCapture")
            .DisableAntiforgery()
            .Accepts<IFormFile>("multipart/form-data")
            .Produces<Capture>(StatusCodes.Status201Created)
            .ProducesValidationProblem();
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

    private static async Task<Results<Created<Capture>, ValidationProblem>> UploadAsync(
        IFormFile? file,
        IUploadPolicy uploadPolicy,
        ICaptureService captureService,
        CancellationToken ct)
    {
        var error = ValidateUpload(file, uploadPolicy);
        if (error is not null)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["file"] = [error],
            });
        }

        await using var stream = file!.OpenReadStream();
        var capture = await captureService.SubmitAsync(
            content: null,
            ChannelKind.Api,
            new AttachmentInput
            {
                Content = stream,
                FileName = file.FileName,
                ContentType = file.ContentType,
                SizeBytes = file.Length,
            },
            ct);

        return TypedResults.Created($"/api/v1/captures/{capture.Id}", capture);
    }

    private static string? ValidateUpload(IFormFile? file, IUploadPolicy uploadPolicy)
    {
        if (file is null || file.Length == 0)
        {
            return "A non-empty file is required.";
        }

        if (file.Length > uploadPolicy.MaxBytes)
        {
            return $"File exceeds the maximum size of {uploadPolicy.MaxBytes} bytes.";
        }

        if (!uploadPolicy.AllowedContentTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            return $"Content type '{file.ContentType}' is not allowed.";
        }

        return null;
    }
}

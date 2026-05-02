using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FlowHub.Api.Endpoints;

public static class CaptureEndpoints
{
    public static IEndpointRouteBuilder MapFlowHubApi(this IEndpointRouteBuilder app)
    {
        var captures = app.MapGroup("/api/v1/captures")
            .RequireAuthorization()
            .WithTags("Captures");

        // Endpoints land in subsequent tasks (T8, T9, T10, T11).

        return app;
    }
}

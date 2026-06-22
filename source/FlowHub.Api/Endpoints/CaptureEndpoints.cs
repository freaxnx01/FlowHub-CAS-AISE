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

        // Endpoint clusters split per concern (issue #95): keep this routing root
        // tiny so it doesn't trip CA1506; each cluster owns its own dependencies.
        captures.MapCaptureReadEndpoints();
        captures.MapCaptureWriteEndpoints();
        captures.MapCaptureRetryEndpoint();

        app.MapSearchEndpoints();
        app.MapAdminEndpoints();

        return app;
    }
}

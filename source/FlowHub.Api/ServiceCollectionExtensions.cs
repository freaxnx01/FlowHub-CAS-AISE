using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace FlowHub.Api;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFlowHubApi(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddProblemDetails();
        services.AddOpenApi();
        return services;
    }
}

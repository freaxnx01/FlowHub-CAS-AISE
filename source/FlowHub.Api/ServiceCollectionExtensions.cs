using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Text.Json.Serialization;

namespace FlowHub.Api;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFlowHubApi(this IServiceCollection services)
    {
        services.ConfigureHttpJsonOptions(o =>
            o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddProblemDetails();
        services.AddOpenApi();
        return services;
    }
}

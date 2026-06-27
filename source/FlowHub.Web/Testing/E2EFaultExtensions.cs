using System.Diagnostics.CodeAnalysis;
using FlowHub.Core.Health;

namespace FlowHub.Web.Testing;

/// <summary>
/// E2E-only DI hook. Activates only when <c>FLOWHUB_E2E_FAULTS_ENABLED=true</c>
/// is set in the environment — invisible in any other deployment.
/// </summary>
// Excluded from coverage: by design only exercised by the E2E browser pipeline
// (see <see cref="IsEnabled"/>); unit-testing it would test the env-var bridge,
// not behaviour.
[ExcludeFromCodeCoverage]
public static class E2EFaultExtensions
{
    public static bool IsEnabled =>
        string.Equals(
            Environment.GetEnvironmentVariable("FLOWHUB_E2E_FAULTS_ENABLED"),
            "true",
            StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Conditional wrapper for <see cref="AddE2EFaultInjection"/>: registers the fault
    /// scaffolding only when <see cref="IsEnabled"/>. Lives next to the env-var bridge
    /// so Program.cs stays free of the <c>if (E2EFaultExtensions.IsEnabled)</c> noise
    /// (and so coverlet doesn't flag those two lines forever).
    /// </summary>
    public static IServiceCollection AddE2EFaultInjectionIfEnabled(this IServiceCollection services)
    {
        if (IsEnabled)
        {
            services.AddE2EFaultInjection();
        }
        return services;
    }

    /// <summary>Conditional wrapper for <see cref="MapE2EFaultEndpoints"/>; same rationale.</summary>
    public static IEndpointRouteBuilder MapE2EFaultEndpointsIfEnabled(this IEndpointRouteBuilder app)
    {
        if (IsEnabled)
        {
            app.MapE2EFaultEndpoints();
        }
        return app;
    }

    public static IServiceCollection AddE2EFaultInjection(this IServiceCollection services)
    {
        services.AddSingleton<IFaultInjector, FaultInjector>();

        // Replace the existing ISkillRegistry / IIntegrationHealthService registrations
        // with decorated versions that consult the IFaultInjector first.
        DecorateScoped<ISkillRegistry, FaultInjectingSkillRegistry>(services,
            (inner, faults) => new FaultInjectingSkillRegistry(inner, faults));
        DecorateScoped<IIntegrationHealthService, FaultInjectingIntegrationHealthService>(services,
            (inner, faults) => new FaultInjectingIntegrationHealthService(inner, faults));

        return services;
    }

    /// <summary>Maps the test-only POST endpoints used by E2E specs to arm / disarm faults.</summary>
    public static IEndpointRouteBuilder MapE2EFaultEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/test/faults").AllowAnonymous();

        group.MapPost("/{name}/arm", (string name, IFaultInjector faults) =>
        {
            switch (name)
            {
                case "skills": faults.SkillsShouldFail = true; return Results.NoContent();
                case "integrations": faults.IntegrationsShouldFail = true; return Results.NoContent();
                default: return Results.NotFound();
            }
        });

        group.MapPost("/{name}/disarm", (string name, IFaultInjector faults) =>
        {
            switch (name)
            {
                case "skills": faults.SkillsShouldFail = false; return Results.NoContent();
                case "integrations": faults.IntegrationsShouldFail = false; return Results.NoContent();
                default: return Results.NotFound();
            }
        });

        return app;
    }

    private static void DecorateScoped<TService, TDecorator>(
        IServiceCollection services,
        Func<TService, IFaultInjector, TService> factory)
        where TService : class
        where TDecorator : class, TService
    {
        var existing = services.LastOrDefault(d => d.ServiceType == typeof(TService))
            ?? throw new InvalidOperationException(
                $"AddE2EFaultInjection requires {typeof(TService).Name} to be registered first.");

        services.Remove(existing);
        services.AddScoped(typeof(TService), sp =>
        {
            var inner = (TService)ActivatorUtilities.CreateInstance(sp, existing.ImplementationType!);
            var faults = sp.GetRequiredService<IFaultInjector>();
            return factory(inner, faults);
        });
    }
}

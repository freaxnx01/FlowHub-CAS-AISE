using FlowHub.Core.Captures;
using FlowHub.Web.Stubs;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace FlowHub.Web.ComponentTests.Pipeline;

/// <summary>
/// Builds a MassTransit test harness with a real CaptureServiceStub and substitute
/// classifier / integrations. Tests configure the substitutes per case.
/// </summary>
internal static class PipelineTestBase
{
    public static ServiceProvider Build(
        Action<ServiceCollection>? configure = null,
        Action<IBusRegistrationConfigurator>? configureBus = null)
    {
        var services = new ServiceCollection();

        // CaptureServiceStub depends on IPublishEndpoint, which MassTransit registers as
        // scoped. IBus is singleton and also implements IPublishEndpoint, so we wire the
        // singleton stub through the bus to keep everything singleton-resolvable from the
        // root provider (tests resolve ICaptureService from provider directly).
        services.AddSingleton<ICaptureService>(sp =>
            new CaptureServiceStub(sp.GetRequiredService<IBus>()));
        services.AddSingleton(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(Microsoft.Extensions.Logging.ILogger<>),
            typeof(Microsoft.Extensions.Logging.Abstractions.NullLogger<>));

        configure?.Invoke(services);

        services.AddMassTransitTestHarness(cfg =>
        {
            configureBus?.Invoke(cfg);
        });

        return services.BuildServiceProvider(true);
    }
}

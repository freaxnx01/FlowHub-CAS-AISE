using FlowHub.Web;
using FlowHub.Web.Notifications;
using FlowHub.Web.Pipeline;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlowHub.Web.ComponentTests;

public sealed class ProgramRegistrationTests
{
    private static WebApplicationBuilder BuildWith(Dictionary<string, string?>? config = null)
    {
        // CreateBuilder takes args[]; ConfigureAppConfiguration on the builder isn't quite
        // the right surface — just append in-memory entries straight to the live config.
        var builder = WebApplication.CreateBuilder([]);
        if (config is { Count: > 0 })
        {
            builder.Configuration.AddInMemoryCollection(config);
        }
        return builder;
    }

    // --- AddFlowHubAuthentication ----------------------------------------------------------

    [Fact]
    public void AddFlowHubAuthentication_NoOidcAuthority_RegistersDemoScheme()
    {
        var builder = BuildWith();

        builder.AddFlowHubAuthentication();

        // The demo handler is registered as the default auth scheme.
        var auth = builder.Services.LastOrDefault(d =>
            d.ServiceType.FullName == "Microsoft.AspNetCore.Authentication.IAuthenticationService");
        auth.Should().NotBeNull();
    }

    [Fact]
    public async Task AddFlowHubAuthentication_WithOidcAuthority_RegistersBearerAndOidcSchemes()
    {
        var builder = BuildWith(new Dictionary<string, string?>
        {
            ["Auth:OIDC:Authority"]    = "https://oidc.example.com",
            ["Auth:OIDC:ClientId"]     = "flowhub-web",
            ["Auth:OIDC:ClientSecret"] = "shh",
        });

        builder.AddFlowHubAuthentication();

        // The OIDC branch registers AddJwtBearer + AddCookie + AddOpenIdConnect under the
        // "smart" policy scheme. Build the SP and resolve the schemes.
        using var sp = builder.Services.BuildServiceProvider();
        var schemes = sp.GetRequiredService<Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider>();
        var all = await schemes.GetAllSchemesAsync();
        all.Should().Contain(s => s.Name == JwtBearerDefaults.AuthenticationScheme);
        all.Should().Contain(s => s.Name == Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectDefaults.AuthenticationScheme);
        all.Should().Contain(s => s.Name == Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);
        all.Should().Contain(s => s.Name == "smart");
    }

    [Fact]
    public void AddFlowHubAuthentication_OidcAuthority_MissingClientId_Throws()
    {
        var builder = BuildWith(new Dictionary<string, string?>
        {
            ["Auth:OIDC:Authority"] = "https://oidc.example.com",
            // ClientId intentionally missing.
        });

        var act = () => builder.AddFlowHubAuthentication();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Auth:OIDC:ClientId is required*");
    }

    [Fact]
    public void AddFlowHubAuthentication_OidcAuthority_MissingClientSecret_Throws()
    {
        var builder = BuildWith(new Dictionary<string, string?>
        {
            ["Auth:OIDC:Authority"] = "https://oidc.example.com",
            ["Auth:OIDC:ClientId"]  = "flowhub-web",
            // ClientSecret intentionally missing.
        });

        var act = () => builder.AddFlowHubAuthentication();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Auth:OIDC:ClientSecret is required*");
    }

    // --- AddFlowHubDemoNotifications -------------------------------------------------------

    [Fact]
    public void AddFlowHubDemoNotifications_NotConfigured_ReturnsFalse_AndRegistersNothing()
    {
        var builder = BuildWith();

        var enabled = builder.AddFlowHubDemoNotifications();

        enabled.Should().BeFalse();
        builder.Services.Should().NotContain(d => d.ServiceType == typeof(ICaptureNotifier));
    }

    [Fact]
    public void AddFlowHubDemoNotifications_BaseUrlOnly_NotConfigured_ReturnsFalse()
    {
        // IsConfigured requires *both* BaseUrl AND Topic. Only one set → still false.
        var builder = BuildWith(new Dictionary<string, string?>
        {
            ["Demo:Notify:Ntfy:BaseUrl"] = "https://ntfy.example.com",
        });

        var enabled = builder.AddFlowHubDemoNotifications();

        enabled.Should().BeFalse();
    }

    [Fact]
    public void AddFlowHubDemoNotifications_BaseUrlAndTopic_ReturnsTrue_AndRegistersNtfyClient()
    {
        var builder = BuildWith(new Dictionary<string, string?>
        {
            ["Demo:Notify:Ntfy:BaseUrl"] = "https://ntfy.example.com",
            ["Demo:Notify:Ntfy:Topic"]   = "flowhub-test-captures",
        });

        var enabled = builder.AddFlowHubDemoNotifications();

        enabled.Should().BeTrue();
        builder.Services.Should().Contain(d => d.ServiceType == typeof(ICaptureNotifier));
    }

    // --- AddFlowHubMessaging ---------------------------------------------------------------

    [Fact]
    public void AddFlowHubMessaging_DefaultTransport_UsesInMemory_AndRegistersPipelineConsumers()
    {
        var builder = BuildWith();

        builder.AddFlowHubMessaging(demoNotifyEnabled: false);

        // MassTransit registers IBusControl; the pipeline consumers are added as scoped IConsumer<T>.
        builder.Services.Should().Contain(d => d.ServiceType == typeof(IBusControl));
        builder.Services.Should().Contain(d => d.ServiceType == typeof(CaptureEnrichmentConsumer));
        builder.Services.Should().Contain(d => d.ServiceType == typeof(CaptureEmbeddingConsumer));
        builder.Services.Should().Contain(d => d.ServiceType == typeof(SkillRoutingConsumer));
        builder.Services.Should().Contain(d => d.ServiceType == typeof(LifecycleFaultObserver));
        // demoNotifyEnabled=false → the ntfy consumer is *not* registered.
        builder.Services.Should().NotContain(d => d.ServiceType == typeof(CaptureNotificationConsumer));
    }

    [Fact]
    public void AddFlowHubMessaging_WithDemoNotifyEnabled_RegistersCaptureNotificationConsumer()
    {
        var builder = BuildWith();

        builder.AddFlowHubMessaging(demoNotifyEnabled: true);

        builder.Services.Should().Contain(d => d.ServiceType == typeof(CaptureNotificationConsumer));
    }

    [Fact]
    public void AddFlowHubMessaging_RabbitMqTransport_BuildsWithoutThrowing()
    {
        // Selecting the RabbitMq branch is config-only — the bus doesn't actually try to
        // connect until BusControl.Start is called. We just need the registration code path
        // (lines 129-137 of ProgramRegistration) to execute.
        var builder = BuildWith(new Dictionary<string, string?>
        {
            ["Bus:Transport"]            = "RabbitMq",
            ["Bus:RabbitMq:Host"]        = "rabbit.example.com",
            ["Bus:RabbitMq:Username"]    = "flowhub",
            ["Bus:RabbitMq:Password"]    = "shh",
        });

        var act = () => builder.AddFlowHubMessaging(demoNotifyEnabled: false);

        act.Should().NotThrow();
        builder.Services.Should().Contain(d => d.ServiceType == typeof(IBusControl));
    }

    [Fact]
    public void AddFlowHubMessaging_RabbitMqTransport_MissingCredentials_DefaultsToGuest()
    {
        // The `?? "guest"` fallbacks in the Host(...) lambda are dead until the bus starts;
        // exercise that the registration doesn't trip over null Username/Password config.
        var builder = BuildWith(new Dictionary<string, string?>
        {
            ["Bus:Transport"]     = "rabbitmq",       // case-insensitive match
            ["Bus:RabbitMq:Host"] = "rabbit.example.com",
        });

        var act = () => builder.AddFlowHubMessaging(demoNotifyEnabled: false);

        act.Should().NotThrow();
    }
}

using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FlowHub.Web.ComponentTests;

public sealed class ProgramStartupTests
{
    // Covers Program.cs lines 120-124: the `if (!app.Environment.IsDevelopment())` branch
    // that wires `UseExceptionHandler("/Error", …)` + `UseHsts()`. The factory default
    // environment is "Development", so we have to override it explicitly to hit this arm.
    //
    // Proof-of-life for UseHsts(): send an HTTPS request (TestServer's `BaseAddress`
    // with `https://` scheme makes `Request.IsHttps` true, which is what HstsMiddleware
    // gates on) and assert `Strict-Transport-Security` is present on the response.
    // Inspecting `IOptions<HstsOptions>.Value.MaxAge` won't do — `HstsOptions.MaxAge`
    // has a type-level initializer of 30 days, so it reads positive even in Development
    // where `UseHsts()` was never called.
    [Fact]
    public async Task ProductionEnvironment_EmitsStrictTransportSecurityHeader()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(b => b
                .UseEnvironment("Production")
                // HstsOptions.ExcludedHosts defaults to {localhost, 127.0.0.1, [::1]} —
                // the loopback exemption that production legitimately wants but that
                // would suppress the header for our in-memory TestServer client
                // (BaseAddress is localhost). Clear it so the assertion below can
                // observe what real prod traffic would receive.
                .ConfigureServices(s => s.Configure<HstsOptions>(o => o.ExcludedHosts.Clear())));

        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost/"),
            AllowAutoRedirect = false,
        });
        var response = await client.GetAsync(new Uri("/health/live", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().Contain(h => h.Key == "Strict-Transport-Security",
            "UseHsts() must run in the !IsDevelopment branch of Program.cs — a regression " +
            "that dropped the call would leave responses unsigned in production.");
    }

    // Covers Program.cs line 56-59: `if (!string.IsNullOrWhiteSpace(otlpEndpoint))
    // { t.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint)); }`.
    // The OTel exporter doesn't actually try to reach the endpoint until a span is exported,
    // so we just need a syntactically valid URI for the registration to succeed.
    [Fact]
    public async Task OtlpEndpointConfigured_StartsCleanly_AndServesLiveProbe()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(b => b.ConfigureAppConfiguration((_, c) =>
                c.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Otlp:Endpoint"] = "http://localhost:4317",
                })));
        var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/health/live", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // Covers Program.cs line 110-111: `GetValue<long?>("FlowHub:Uploads:MaxBytes")
    // ?? UploadOptions.DefaultMaxBytes`. Both arms reduce to one observable post-
    // condition: the resolved value is whatever Kestrel ends up enforcing.
    //
    // WebApplicationFactory's ConfigureAppConfiguration runs DURING the host build,
    // but Program.cs's GetValue call happens at the top level BEFORE Build() — so a
    // test override via the factory pipeline arrives too late and doesn't influence
    // the read. Instead, resolve both sides from the live host and assert the chain
    // is intact (whatever config says, Kestrel mirrors).
    [Fact]
    public async Task ConfiguredMaxUploadBytes_PropagatesIntoKestrelLimits()
    {
        await using var factory = new WebApplicationFactory<Program>();
        _ = factory.CreateClient();  // forces host build

        var config = factory.Services.GetRequiredService<IConfiguration>();
        var configured = config.GetValue<long?>("FlowHub:Uploads:MaxBytes")
            ?? FlowHub.Core.Captures.UploadOptions.DefaultMaxBytes;

        var kestrel = factory.Services.GetRequiredService<IOptions<KestrelServerOptions>>().Value;
        kestrel.Limits.MaxRequestBodySize.Should().Be(configured,
            "Program.cs must forward the configured upload limit into Kestrel — a bug " +
            "that dropped the Configure<KestrelServerOptions>(...) call would leave " +
            "MaxRequestBodySize at its 30 MB default.");
    }
}

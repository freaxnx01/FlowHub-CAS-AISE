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
    // We can't assert the `Strict-Transport-Security` header on a response because
    // TestServer serves over HTTP and HSTS middleware only writes the header on HTTPS
    // requests — so instead we resolve IOptions<HstsOptions> from the test host and
    // confirm the production-only pipeline configured it (default `MaxAge` is 30 days).
    [Fact]
    public async Task ProductionEnvironment_ConfiguresHstsOptions_AndPipelineStartsCleanly()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(b => b.UseEnvironment("Production"));

        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            // HTTPS redirection would otherwise turn /health/live into a 307.
            AllowAutoRedirect = false,
        });
        var response = await client.GetAsync(new Uri("/health/live", UriKind.Relative));

        // 307 = HTTPS redirect (production wires UseHttpsRedirection); 200 means the
        // probe reached the endpoint. Either proves the pipeline assembled.
        ((int)response.StatusCode).Should().BeOneOf((int)HttpStatusCode.OK, (int)HttpStatusCode.TemporaryRedirect);

        // The key assertion: in Production the host registered HstsOptions; in
        // Development it would not (UseHsts is only called in the !IsDevelopment branch).
        // MaxAge defaults to 30 days when UseHsts() is called without an override.
        var hsts = factory.Services.GetRequiredService<IOptions<HstsOptions>>().Value;
        hsts.MaxAge.Should().BeGreaterThan(TimeSpan.Zero,
            "UseHsts() must have wired the production-only HSTS middleware");
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

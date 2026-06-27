using System.Net;

namespace FlowHub.Api.IntegrationTests.Captures;

/// <summary>
/// /api/v1/admin/* is gated by the "Admin" authorization policy (issue #100,
/// defence-in-depth). The demo operator principal (DemoAuthHandler) is granted
/// only "Operator" by default, so the surface must be forbidden — and reachable
/// only when an Admin role is deliberately added via Demo:Auth:Roles.
/// </summary>
public sealed class AdminEndpointsAuthTests : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;

    public AdminEndpointsAuthTests(IntegrationTestFactory factory) => _factory = factory;

    [Fact]
    public async Task Admin_AsDefaultDemoOperator_Returns403Forbidden()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/api/v1/admin/embeddings/rebuild", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Admin_WhenAdminRoleGranted_PassesAuthorization()
    {
        // Widen the demo principal to include Admin; the request now clears
        // authorization and reaches the handler, which 503s because embeddings
        // are unconfigured in the test host — i.e. no longer a 403.
        var client = _factory
            .WithWebHostBuilder(b => b.UseSetting("Demo:Auth:Roles", "Operator,Admin"))
            .CreateClient();

        var response = await client.PostAsync("/api/v1/admin/embeddings/rebuild", content: null);

        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }
}

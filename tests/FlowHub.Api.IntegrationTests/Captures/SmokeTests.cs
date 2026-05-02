namespace FlowHub.Api.IntegrationTests.Captures;

public sealed class SmokeTests : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;

    public SmokeTests(IntegrationTestFactory factory) => _factory = factory;

    [Fact]
    public async Task Scalar_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/scalar");
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }

    [Fact]
    public async Task OpenApiDocument_IsServed()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/openapi/v1.json");
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("\"openapi\"");
    }
}

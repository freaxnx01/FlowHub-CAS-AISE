using FlowHub.Core.Captures;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using System.Net;
using FluentAssertions;

namespace FlowHub.Web.ComponentTests.Api;

public sealed class SearchEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SearchEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Search_WhenEmbeddingServiceNotConfigured_Returns503()
    {
        var client = _factory.WithWebHostBuilder(b =>
        {
            b.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IEmbeddingService));
                if (descriptor is not null) services.Remove(descriptor);
                var svc = Substitute.For<IEmbeddingService>();
                svc.GenerateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                    .Returns(Task.FromResult<float[]?>(null));
                services.AddSingleton(svc);
            });
        }).CreateClient();

        var response = await client.GetAsync("/api/v1/captures/search?q=test");

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task Search_WhenEmbeddingServiceConfigured_Returns200WithResults()
    {
        var fakeEmbedding = Enumerable.Range(0, 384).Select(i => (float)i / 384).ToArray();
        var fakeCaptures = new List<Capture>
        {
            new(Guid.NewGuid(), ChannelKind.Api, "Test content", DateTimeOffset.UtcNow, LifecycleStage.Completed, "test-skill")
        };

        var client = _factory.WithWebHostBuilder(b =>
        {
            b.ConfigureServices(services =>
            {
                var embSvc = Substitute.For<IEmbeddingService>();
                embSvc.GenerateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                    .Returns(Task.FromResult<float[]?>(fakeEmbedding));

                var repo = Substitute.For<ICaptureRepository>();
                repo.SearchByEmbeddingAsync(Arg.Any<float[]>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                    .Returns(Task.FromResult<IReadOnlyList<Capture>>(fakeCaptures));

                var embDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IEmbeddingService));
                if (embDescriptor is not null) services.Remove(embDescriptor);
                services.AddSingleton(embSvc);

                var repoDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ICaptureRepository));
                if (repoDescriptor is not null) services.Remove(repoDescriptor);
                services.AddScoped(_ => repo);
            });
        }).CreateClient();

        var response = await client.GetAsync("/api/v1/captures/search?q=database+performance&limit=5");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Test content");
    }
}

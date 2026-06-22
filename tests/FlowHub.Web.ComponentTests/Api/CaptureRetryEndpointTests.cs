using System.Net;
using FlowHub.Core.Events;
using MassTransit;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace FlowHub.Web.ComponentTests.Api;

public sealed class CaptureRetryEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CaptureRetryEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Retry_UnknownId_Returns404()
    {
        var captures = Substitute.For<ICaptureService>();
        captures.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Capture?>(null));
        var client = WithCaptures(captures).CreateClient();

        var response = await client.PostAsync($"/api/v1/captures/{Guid.NewGuid()}/retry", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Retry_NonRetryableStage_Returns409()
    {
        var capture = MakeCapture(LifecycleStage.Completed);
        var captures = Substitute.For<ICaptureService>();
        captures.GetByIdAsync(capture.Id, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Capture?>(capture));
        var client = WithCaptures(captures).CreateClient();

        var response = await client.PostAsync($"/api/v1/captures/{capture.Id}/retry", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Theory]
    [InlineData(LifecycleStage.Orphan)]
    [InlineData(LifecycleStage.Unhandled)]
    public async Task Retry_RetryableStage_Returns202AndPublishesCaptureCreated(LifecycleStage stage)
    {
        var capture = MakeCapture(stage);
        var captures = Substitute.For<ICaptureService>();
        captures.GetByIdAsync(capture.Id, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Capture?>(capture));
        captures.ResetForRetryAsync(capture.Id, Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);
        var bus = Substitute.For<IBus>();
        var client = WithCapturesAndBus(captures, bus).CreateClient();

        var response = await client.PostAsync($"/api/v1/captures/{capture.Id}/retry", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        await captures.Received(1).ResetForRetryAsync(capture.Id, Arg.Any<CancellationToken>());
        await bus.Received(1).Publish(Arg.Any<CaptureCreated>(), Arg.Any<CancellationToken>());
    }

    private WebApplicationFactory<Program> WithCaptures(ICaptureService captures) =>
        _factory.WithWebHostBuilder(b => b.ConfigureServices(services =>
        {
            Replace(services, captures);
        }));

    private WebApplicationFactory<Program> WithCapturesAndBus(ICaptureService captures, IBus bus) =>
        _factory.WithWebHostBuilder(b => b.ConfigureServices(services =>
        {
            Replace(services, captures);
            Replace(services, bus);
        }));

    private static void Replace<T>(IServiceCollection services, T impl) where T : class
    {
        foreach (var d in services.Where(d => d.ServiceType == typeof(T)).ToList()) services.Remove(d);
        services.AddSingleton(impl);
    }

    private static Capture MakeCapture(LifecycleStage stage) => new(
        Id: Guid.NewGuid(),
        Source: ChannelKind.Api,
        Content: "https://example.com",
        CreatedAt: DateTimeOffset.UtcNow,
        Stage: stage,
        MatchedSkill: stage == LifecycleStage.Completed ? "Wallabag" : null);
}

using System.Net;
using FlowHub.Core.Events;
using FlowHub.Web.Notifications;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FlowHub.Web.ComponentTests.Notifications;

public class NtfyCaptureNotifierTests
{
    [Fact]
    public async Task Notify_PostsToTopic_WithBody_Title_AndBearerAuth()
    {
        var handler = new CapturingHandler();
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://ntfy.example.com/") };
        var options = Options.Create(new DemoNotifyOptions
        {
            BaseUrl = "https://ntfy.example.com",
            Topic = "flowhub-demo",
            AccessToken = "tk_secret",
        });
        var notifier = new NtfyCaptureNotifier(http, options, NullLogger<NtfyCaptureNotifier>.Instance);

        await notifier.NotifyCaptureCreatedAsync(
            new CaptureCreated(Guid.NewGuid(), "https://example.com/article", ChannelKind.Web, DateTimeOffset.UtcNow),
            CancellationToken.None);

        handler.Request!.Method.Should().Be(HttpMethod.Post);
        handler.Request.RequestUri!.AbsoluteUri.Should().Be("https://ntfy.example.com/flowhub-demo");
        handler.Body.Should().Contain("Web:").And.Contain("https://example.com/article");
        handler.Request.Headers.GetValues("Title").Should().Contain("New FlowHub demo capture");
        handler.Request.Headers.GetValues("Authorization").Should().Contain("Bearer tk_secret");
    }

    [Fact]
    public async Task Notify_ServerError_DoesNotThrow()
    {
        var handler = new CapturingHandler { Status = HttpStatusCode.InternalServerError };
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://ntfy.example.com/") };
        var options = Options.Create(new DemoNotifyOptions { BaseUrl = "https://ntfy.example.com", Topic = "t" });
        var notifier = new NtfyCaptureNotifier(http, options, NullLogger<NtfyCaptureNotifier>.Instance);

        var act = async () => await notifier.NotifyCaptureCreatedAsync(
            new CaptureCreated(Guid.NewGuid(), "x", ChannelKind.Api, DateTimeOffset.UtcNow), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }
        public string? Body { get; private set; }
        public HttpStatusCode Status { get; init; } = HttpStatusCode.OK;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(Status);
        }
    }
}

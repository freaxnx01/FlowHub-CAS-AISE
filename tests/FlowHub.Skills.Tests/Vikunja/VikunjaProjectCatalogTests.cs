using FlowHub.Skills.Vikunja;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RichardSzalay.MockHttp;

namespace FlowHub.Skills.Tests.Vikunja;

public sealed class VikunjaProjectCatalogTests
{
    [Fact]
    public void Dispose_CanBeCalledRepeatedlyWithoutThrowing()
    {
        // Hits the Dispose() line; the SemaphoreSlim's own disposal is idempotent
        // when no awaiter is waiting.
        var http = new MockHttpMessageHandler().ToHttpClient();
        http.BaseAddress = new Uri("https://vikunja.example.com");
        var sut = new VikunjaProjectCatalog(
            http,
            Options.Create(new VikunjaOptions { BaseUrl = "https://vikunja.example.com", ApiToken = "t" }),
            NullLogger<VikunjaProjectCatalog>.Instance,
            TimeProvider.System);

        sut.Dispose();

        var act = sut.Dispose;
        act.Should().NotThrow();
    }
}

using System.Net;
using FlowHub.Core.Captures;
using FlowHub.Skills.Paperless;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RichardSzalay.MockHttp;

namespace FlowHub.Skills.Tests.Paperless;

public sealed class PaperlessSkillIntegrationTests
{
    private static (PaperlessSkillIntegration sut, MockHttpMessageHandler mock, IAttachmentStorage storage) Build()
    {
        var options = new PaperlessOptions { BaseUrl = "https://paperless.example.com", ApiToken = "tok" };
        var mock = new MockHttpMessageHandler();
        var http = mock.ToHttpClient();
        http.BaseAddress = new Uri(options.BaseUrl!);
        var storage = Substitute.For<IAttachmentStorage>();
        var sut = new PaperlessSkillIntegration(http, Options.Create(options), storage, NullLogger<PaperlessSkillIntegration>.Instance);
        return (sut, mock, storage);
    }

    private static Capture DocCapture(Attachment? attachment) => new(
        Id: Guid.NewGuid(),
        Source: ChannelKind.Web,
        Content: "scan.pdf",
        CreatedAt: DateTimeOffset.UtcNow,
        Stage: LifecycleStage.Classified,
        MatchedSkill: "Paperless",
        Attachment: attachment);

    [Fact]
    public void Name_IsPaperless()
    {
        var (sut, _, _) = Build();
        sut.Name.Should().Be("Paperless");
    }

    [Fact]
    public async Task HandleAsync_PostsMultipartWithTokenHeader_ReturnsExternalRef()
    {
        var (sut, mock, storage) = Build();
        storage.OpenReadAsync("2026/06/abc.pdf", Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult<Stream>(new MemoryStream(new byte[] { 1, 2, 3 })));

        mock.Expect(HttpMethod.Post, "https://paperless.example.com/api/documents/post_document/")
            .WithHeaders("Authorization", "Token tok")
            .WithPartialContent("name=document")
            .WithPartialContent("name=title")
            .Respond("application/json", "\"d9b8...uuid\"");

        var capture = DocCapture(new Attachment("scan.pdf", "application/pdf", 3, "2026/06/abc.pdf", DateTimeOffset.UtcNow));

        var result = await sut.HandleAsync(capture, default);

        result.Success.Should().BeTrue();
        result.ExternalRef.Should().Be("d9b8...uuid");
        mock.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task HandleAsync_NoAttachment_Throws()
    {
        var (sut, _, _) = Build();
        var act = () => sut.HandleAsync(DocCapture(attachment: null), default);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task HandleAsync_ServerReturns503_ThrowsHttpRequestException()
    {
        var (sut, mock, storage) = Build();
        storage.OpenReadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult<Stream>(new MemoryStream(new byte[] { 1 })));
        mock.Expect(HttpMethod.Post, "*/api/documents/post_document/").Respond(HttpStatusCode.ServiceUnavailable);

        var capture = DocCapture(new Attachment("scan.pdf", "application/pdf", 1, "p.pdf", DateTimeOffset.UtcNow));
        var act = () => sut.HandleAsync(capture, default);
        await act.Should().ThrowAsync<HttpRequestException>();
    }
}

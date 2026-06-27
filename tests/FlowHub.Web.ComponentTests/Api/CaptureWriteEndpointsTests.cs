using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FlowHub.Api.Requests;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace FlowHub.Web.ComponentTests.Api;

public sealed class CaptureWriteEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CaptureWriteEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Submit_InvalidPayload_ReturnsValidationProblem()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/captures",
            new CreateCaptureRequest("", ChannelKind.Api));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Content");
    }

    [Fact]
    public async Task Upload_ZeroLengthFile_ReturnsValidationProblem()
    {
        // Drive the "file is null or empty" arm of ValidateUpload by sending a
        // multipart payload that contains a file part of length 0 — ASP.NET binds
        // it, but our handler's first guard rejects it as non-empty-required.
        var client = _factory.CreateClient();
        using var content = new MultipartFormDataContent();
        var emptyFile = new ByteArrayContent([]);
        emptyFile.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        content.Add(emptyFile, "file", "empty.txt");

        var response = await client.PostAsync("/api/v1/captures/upload", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("non-empty file");
    }

    [Fact]
    public async Task Upload_ExceedsMaxBytes_ReturnsValidationProblem()
    {
        // Shrink the upload policy via DI so we don't have to send a giant payload.
        var client = _factory.WithWebHostBuilder(b =>
        {
            b.ConfigureServices(services =>
            {
                var policy = Substitute.For<IUploadPolicy>();
                policy.MaxBytes.Returns(8L);
                policy.AllowedContentTypes.Returns(new[] { "text/plain" });
                policy.AcceptAttribute.Returns(".txt");
                services.AddSingleton(policy);
            });
        }).CreateClient();

        using var content = new MultipartFormDataContent();
        var bytes = new byte[64];
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        content.Add(fileContent, "file", "too-big.txt");

        var response = await client.PostAsync("/api/v1/captures/upload", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("maximum size");
    }

    [Fact]
    public async Task Upload_ValidFile_Returns201Created()
    {
        // Drives the ValidateUpload success path (returns null) end-to-end.
        var capture = new Capture(
            Id: Guid.NewGuid(),
            Source: ChannelKind.Api,
            Content: "hello.txt",
            CreatedAt: DateTimeOffset.UtcNow,
            Stage: LifecycleStage.Raw,
            MatchedSkill: null);
        var captures = Substitute.For<ICaptureService>();
        captures.SubmitAsync(Arg.Any<string?>(), Arg.Any<ChannelKind>(), Arg.Any<AttachmentInput>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(capture));

        var client = _factory.WithWebHostBuilder(b =>
        {
            b.ConfigureServices(services =>
            {
                foreach (var d in services.Where(d => d.ServiceType == typeof(ICaptureService)).ToList()) services.Remove(d);
                services.AddSingleton(captures);

                var policy = Substitute.For<IUploadPolicy>();
                policy.MaxBytes.Returns(1_000_000L);
                policy.AllowedContentTypes.Returns(new[] { "text/plain" });
                policy.AcceptAttribute.Returns(".txt");
                services.AddSingleton(policy);
            });
        }).CreateClient();

        using var content = new MultipartFormDataContent();
        var bytes = System.Text.Encoding.UTF8.GetBytes("hello");
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        content.Add(fileContent, "file", "hello.txt");

        var response = await client.PostAsync("/api/v1/captures/upload", content);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        await captures.Received(1).SubmitAsync(
            null,
            ChannelKind.Api,
            Arg.Any<AttachmentInput>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Upload_DisallowedContentType_ReturnsValidationProblem()
    {
        var client = _factory.WithWebHostBuilder(b =>
        {
            b.ConfigureServices(services =>
            {
                var policy = Substitute.For<IUploadPolicy>();
                policy.MaxBytes.Returns(1_000_000L);
                policy.AllowedContentTypes.Returns(new[] { "application/pdf" });
                policy.AcceptAttribute.Returns(".pdf");
                services.AddSingleton(policy);
            });
        }).CreateClient();

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent([1, 2, 3]);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(fileContent, "file", "rogue.png");

        var response = await client.PostAsync("/api/v1/captures/upload", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("not allowed");
    }
}

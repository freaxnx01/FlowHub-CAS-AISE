using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FlowHub.Core.Captures;

namespace FlowHub.Api.IntegrationTests.Captures;

public sealed class UploadCaptureTests : IClassFixture<IntegrationTestFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
        PropertyNameCaseInsensitive = true,
    };

    private readonly IntegrationTestFactory _factory;

    public UploadCaptureTests(IntegrationTestFactory factory) => _factory = factory;

    private static MultipartFormDataContent FileContent(byte[] bytes, string fileName, string contentType)
    {
        var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(file, "file", fileName);
        return content;
    }

    [Fact]
    public async Task Post_ValidPdf_Returns201WithAttachmentCapture()
    {
        var client = _factory.CreateClient();
        using var body = FileContent([0x25, 0x50, 0x44, 0x46], "scan.pdf", "application/pdf"); // %PDF

        var response = await client.PostAsync("/api/v1/captures/upload", body);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location!.ToString().Should().StartWith("/api/v1/captures/");

        var capture = await response.Content.ReadFromJsonAsync<Capture>(JsonOptions);
        capture.Should().NotBeNull();
        capture!.Source.Should().Be(ChannelKind.Api);
        capture.Stage.Should().Be(LifecycleStage.Raw);
        capture.Attachment.Should().NotBeNull();
        capture.Attachment!.FileName.Should().Be("scan.pdf");
        capture.Attachment.ContentType.Should().Be("application/pdf");
    }

    [Fact]
    public async Task Post_DisallowedContentType_Returns400()
    {
        var client = _factory.CreateClient();
        using var body = FileContent([1, 2, 3], "evil.exe", "application/x-msdownload");

        var response = await client.PostAsync("/api/v1/captures/upload", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        (await response.Content.ReadAsStringAsync()).Should().Contain("file");
    }

    [Fact]
    public async Task Post_OversizeFile_Returns400()
    {
        var client = _factory.CreateClient();
        // Default UploadOptions.MaxBytes is 2 MiB; exceed it.
        using var body = FileContent(new byte[(2 * 1024 * 1024) + 1], "big.png", "image/png");

        var response = await client.PostAsync("/api/v1/captures/upload", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_NoFile_Returns400()
    {
        var client = _factory.CreateClient();
        using var body = new MultipartFormDataContent(); // no "file" part

        var response = await client.PostAsync("/api/v1/captures/upload", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}

using FlowHub.Web.Components.Layout;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

namespace FlowHub.Web.ComponentTests.Layout;

public class QuickCaptureFieldUploadTests : TestContext
{
    public QuickCaptureFieldUploadTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public async Task StagingValidFile_AndSubmitting_PassesAttachmentToCaptureService()
    {
        var capture = Substitute.For<ICaptureService>();
        capture.SubmitAsync(Arg.Any<string?>(), ChannelKind.Web, Arg.Any<AttachmentInput?>(), Arg.Any<CancellationToken>())
            .Returns(ci => Task.FromResult(new Capture(
                Guid.NewGuid(), ChannelKind.Web, ci.ArgAt<AttachmentInput>(2)!.FileName,
                DateTimeOffset.UtcNow, LifecycleStage.Raw, null)));
        var policy = Substitute.For<IUploadPolicy>();
        policy.MaxBytes.Returns(2_097_152);
        policy.AllowedContentTypes.Returns(new[] { "application/pdf" });
        policy.AcceptAttribute.Returns("application/pdf");

        Services.AddSingleton(capture);
        Services.AddSingleton(policy);

        var cut = RenderComponent<QuickCaptureField>();
        var file = InputFileContent.CreateFromBinary(new byte[8], "invoice.pdf", null, "application/pdf");
        cut.FindComponent<InputFile>().UploadFiles(file);

        await cut.InvokeAsync(() => cut.Find("button[aria-label='Submit capture']").Click());

        await capture.Received(1).SubmitAsync(
            Arg.Any<string?>(), ChannelKind.Web,
            Arg.Is<AttachmentInput?>(a => a != null && a.FileName == "invoice.pdf" && a.SizeBytes == 8),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void StagingFileExceedingPolicy_DisablesSubmitAndShowsError()
    {
        var capture = Substitute.For<ICaptureService>();
        var policy = Substitute.For<IUploadPolicy>();
        policy.MaxBytes.Returns(4L);
        policy.AllowedContentTypes.Returns(new[] { "application/pdf" });
        policy.AcceptAttribute.Returns("application/pdf");
        Services.AddSingleton(capture);
        Services.AddSingleton(policy);

        var cut = RenderComponent<QuickCaptureField>();
        var file = InputFileContent.CreateFromBinary(new byte[5], "big.pdf", null, "application/pdf");
        cut.FindComponent<InputFile>().UploadFiles(file);

        cut.Markup.Should().Contain("too large");
        capture.DidNotReceiveWithAnyArgs().SubmitAsync(default, default, default, default);
    }

    [Fact]
    public void StagingFileWithDisallowedContentType_ShowsTypeNotAllowedError_AndBlocksSubmit()
    {
        // Drives the second arm of ValidateFile — Size is OK but ContentType isn't
        // in AllowedContentTypes — so we hit the "Type X not allowed" return.
        var capture = Substitute.For<ICaptureService>();
        var policy = Substitute.For<IUploadPolicy>();
        policy.MaxBytes.Returns(1_048_576L);
        policy.AllowedContentTypes.Returns(new[] { "application/pdf" });
        policy.AcceptAttribute.Returns("application/pdf");
        Services.AddSingleton(capture);
        Services.AddSingleton(policy);

        var cut = RenderComponent<QuickCaptureField>();
        var file = InputFileContent.CreateFromBinary(new byte[8], "evil.exe", null, "application/octet-stream");
        cut.FindComponent<InputFile>().UploadFiles(file);

        cut.Markup.Should().Contain("Type application/octet-stream not allowed");
        capture.DidNotReceiveWithAnyArgs().SubmitAsync(default, default, default, default);
    }
}

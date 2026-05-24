using FlowHub.Web.Components.Layout;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using NSubstitute;

namespace FlowHub.Web.ComponentTests.Layout;

public class QuickCaptureFieldTests : TestContext
{
    private readonly ICaptureService _captureService = Substitute.For<ICaptureService>();

    public QuickCaptureFieldTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton(_captureService);

        var policy = Substitute.For<IUploadPolicy>();
        policy.MaxBytes.Returns(2_097_152L);
        policy.AllowedContentTypes.Returns(new[] { "application/pdf" });
        policy.AcceptAttribute.Returns(string.Empty);
        Services.AddSingleton(policy);

        RenderComponent<MudPopoverProvider>();
    }

    [Fact]
    public void Render_ShowsQuickCapturePlaceholder()
    {
        var cut = RenderComponent<QuickCaptureField>();

        cut.Markup.Should().Contain("Quick capture");
    }

    [Fact]
    public void Submit_EmptyInput_DoesNotCallCaptureService()
    {
        var cut = RenderComponent<QuickCaptureField>();

        cut.Find("input[type='text']").TriggerEvent("onkeydown", new KeyboardEventArgs { Key = "Enter" });

        _captureService.DidNotReceive().SubmitAsync(
            Arg.Any<string>(), Arg.Any<ChannelKind>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Submit_NonEmptyInput_CallsCaptureService_WithWebChannel()
    {
        _captureService.SubmitAsync(Arg.Any<string>(), Arg.Any<ChannelKind>(), Arg.Any<CancellationToken>())
            .Returns(new Capture(Guid.NewGuid(), ChannelKind.Web, "https://example.com",
                DateTimeOffset.UtcNow, LifecycleStage.Raw, null));

        var cut = RenderComponent<QuickCaptureField>();
        var input = cut.Find("input[type='text']");
        // MudTextField now uses Immediate="true" → bind on oninput, not onchange.
        input.Input("https://example.com");
        input.TriggerEvent("onkeydown", new KeyboardEventArgs { Key = "Enter" });

        _captureService.Received(1).SubmitAsync(
            "https://example.com", ChannelKind.Web, Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Submit_ServiceThrows_DoesNotCrashComponent()
    {
        _captureService.SubmitAsync(Arg.Any<string>(), Arg.Any<ChannelKind>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<Capture>(new InvalidOperationException("backend down")));

        var cut = RenderComponent<QuickCaptureField>();
        var input = cut.Find("input[type='text']");
        input.Change("anything");
        input.TriggerEvent("onkeydown", new KeyboardEventArgs { Key = "Enter" });

        cut.Markup.Should().Contain("Quick capture");
    }
}

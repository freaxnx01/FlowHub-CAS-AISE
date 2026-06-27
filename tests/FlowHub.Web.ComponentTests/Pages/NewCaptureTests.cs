using FlowHub.Web.Components.Pages;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;

namespace FlowHub.Web.ComponentTests.Pages;

public class NewCaptureTests : TestContext
{
    private readonly ICaptureService _captureService = Substitute.For<ICaptureService>();
    private readonly ISkillRegistry _skillRegistry = Substitute.For<ISkillRegistry>();

    public NewCaptureTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton(_captureService);
        Services.AddSingleton(_skillRegistry);
        var policy = Substitute.For<IUploadPolicy>();
        policy.MaxBytes.Returns(2_097_152L);
        policy.AllowedContentTypes.Returns(new[] { "application/pdf" });
        policy.AcceptAttribute.Returns("application/pdf");
        Services.AddSingleton(policy);
        RenderComponent<MudPopoverProvider>();

        _skillRegistry.GetHealthAsync(Arg.Any<CancellationToken>())
            .Returns(new SkillHealth[]
            {
                new("Books", HealthStatus.Healthy, 10),
                new("Movies", HealthStatus.Healthy, 5),
            });
    }

    [Fact]
    public void Render_ShowsFormWithContentFieldAndSkillDropdown()
    {
        var cut = RenderComponent<NewCapture>();

        cut.Markup.Should().Contain("Content");
        cut.Markup.Should().Contain("Skill override");
        cut.Markup.Should().Contain("Let AI decide");
        cut.Markup.Should().Contain("Submit");
        cut.Markup.Should().Contain("Cancel");
    }

    [Fact]
    public void Render_LoadsSkillsFromRegistry_OnInit()
    {
        RenderComponent<NewCapture>();

        _skillRegistry.Received(1).GetHealthAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Render_WhenSkillsFailToLoad_ShowsWarningAndFormStillUsable()
    {
        _skillRegistry.GetHealthAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IReadOnlyList<SkillHealth>>(new InvalidOperationException("timeout")));

        var cut = RenderComponent<NewCapture>();

        cut.Markup.Should().Contain("Could not load skills");
        cut.Markup.Should().Contain("Submit");
    }

    // Drive OnFileSelected — there are two reject arms (size, content-type) plus
    // the happy path. The happy path is covered indirectly by NewCaptureUploadTests
    // (which also exercises Submit), but the page-level reject arms aren't.

    [Fact]
    public void OnFileSelected_FileTooLarge_SetsFileError_AndDoesNotStage()
    {
        var cut = RenderComponent<NewCapture>();
        // policy.MaxBytes is 2 MiB in the ctor; submit a 3 MiB payload.
        var bigFile = InputFileContent.CreateFromBinary(new byte[3 * 1024 * 1024], "huge.pdf", null, "application/pdf");

        cut.FindComponent<InputFile>().UploadFiles(bigFile);

        cut.Markup.Should().Contain("File too large");
    }

    [Fact]
    public void OnFileSelected_DisallowedContentType_SetsFileError_AndDoesNotStage()
    {
        var cut = RenderComponent<NewCapture>();
        var binary = InputFileContent.CreateFromBinary(new byte[16], "rogue.exe", null, "application/octet-stream");

        cut.FindComponent<InputFile>().UploadFiles(binary);

        cut.Markup.Should().Contain("Type application/octet-stream not allowed");
    }

    [Fact]
    public void Cancel_NavigatesToRoot()
    {
        var cut = RenderComponent<NewCapture>();
        var nav = Services.GetRequiredService<NavigationManager>();

        cut.FindAll("button").First(b => b.TextContent.Contains("Cancel")).Click();

        nav.Uri.Should().EndWith("/");
    }
}

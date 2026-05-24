using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using FlowHub.Web.Components.Pages;

namespace FlowHub.Web.ComponentTests.Pages;

public class NewCaptureUploadTests : TestContext
{
    public NewCaptureUploadTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton(Substitute.For<ICaptureService>());
        var skills = Substitute.For<ISkillRegistry>();
        skills.GetHealthAsync(Arg.Any<CancellationToken>())
              .Returns(Task.FromResult<IReadOnlyList<SkillHealth>>([]));
        Services.AddSingleton(skills);
        var policy = Substitute.For<IUploadPolicy>();
        policy.MaxBytes.Returns(2_097_152);
        policy.AllowedContentTypes.Returns(new[] { "application/pdf" });
        policy.AcceptAttribute.Returns("application/pdf");
        Services.AddSingleton(policy);
        RenderComponent<MudPopoverProvider>();
    }

    [Fact]
    public void StagingFile_DisablesTextAreaAndShowsHelperText()
    {
        var cut = RenderComponent<NewCapture>();
        var file = InputFileContent.CreateFromBinary(new byte[2], "doc.pdf", null, "application/pdf");
        cut.FindComponent<InputFile>().UploadFiles(file);

        cut.Markup.Should().Contain("File overrides text");
        cut.Find("textarea").GetAttribute("disabled").Should().NotBeNull();
    }
}

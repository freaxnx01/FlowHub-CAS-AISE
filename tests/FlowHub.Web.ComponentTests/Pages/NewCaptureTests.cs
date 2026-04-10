using FlowHub.Web.Components.Pages;
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
}

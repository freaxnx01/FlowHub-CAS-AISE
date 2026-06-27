using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using SkillsPage = FlowHub.Web.Components.Pages.Skills;

namespace FlowHub.Web.ComponentTests.Pages;

public class SkillsTests : TestContext
{
    private readonly ISkillRegistry _skillRegistry = Substitute.For<ISkillRegistry>();

    public SkillsTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton(_skillRegistry);
        RenderComponent<MudPopoverProvider>();
    }

    [Fact]
    public void Render_Empty_ShowsConfigurationHint()
    {
        _skillRegistry.GetHealthAsync(Arg.Any<CancellationToken>())
            .Returns(Array.Empty<SkillHealth>());

        var cut = RenderComponent<SkillsPage>();

        cut.Markup.Should().Contain("No skills configured yet");
    }

    [Fact]
    public void Render_WithData_ListsEverySkillNameAndStatus()
    {
        _skillRegistry.GetHealthAsync(Arg.Any<CancellationToken>())
            .Returns(new SkillHealth[]
            {
                new("Books",  HealthStatus.Healthy, 12),
                new("Movies", HealthStatus.Degraded, 3),
            });

        var cut = RenderComponent<SkillsPage>();

        cut.Markup.Should().Contain("Books");
        cut.Markup.Should().Contain("Movies");
        cut.Markup.Should().Contain("healthy");
        cut.Markup.Should().Contain("degraded");
    }

    [Fact]
    public void Render_LoadFailure_ShowsErrorAlertWithRetry()
    {
        _skillRegistry.GetHealthAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IReadOnlyList<SkillHealth>>(new InvalidOperationException("registry down")));

        var cut = RenderComponent<SkillsPage>();

        cut.Markup.Should().Contain("Could not load skills");
        cut.Markup.Should().Contain("registry down");
        cut.Markup.Should().Contain("Retry");
    }

    [Fact]
    public void Render_WhileLoading_ShowsSkeletonPlaceholders()
    {
        // Same pattern as IntegrationsTests.Render_WhileLoading: hold GetHealthAsync
        // pending so the `_skills is null` skeleton block stays rendered.
        var tcs = new TaskCompletionSource<IReadOnlyList<SkillHealth>>();
        _skillRegistry.GetHealthAsync(Arg.Any<CancellationToken>()).Returns(tcs.Task);

        var cut = RenderComponent<SkillsPage>();

        cut.FindAll(".mud-skeleton").Should().NotBeEmpty();
        cut.Markup.Should().NotContain("No skills configured yet");
        cut.Markup.Should().NotContain("Could not load skills");
    }
}

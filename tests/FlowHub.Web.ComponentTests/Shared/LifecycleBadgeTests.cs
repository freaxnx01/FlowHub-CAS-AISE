using FlowHub.Web.Components.Shared;
using MudBlazor.Services;

namespace FlowHub.Web.ComponentTests.Shared;

public class LifecycleBadgeTests : TestContext
{
    public LifecycleBadgeTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Theory]
    [InlineData(LifecycleStage.Raw, "raw")]
    [InlineData(LifecycleStage.Classified, "classified")]
    [InlineData(LifecycleStage.Routed, "routed")]
    [InlineData(LifecycleStage.Completed, "completed")]
    [InlineData(LifecycleStage.Orphan, "orphan")]
    [InlineData(LifecycleStage.Unhandled, "unhandled")]
    public void Render_Stage_ShowsExpectedLabel(LifecycleStage stage, string expectedLabel)
    {
        var cut = RenderComponent<LifecycleBadge>(p => p.Add(c => c.Stage, stage));

        cut.Markup.Should().Contain(expectedLabel);
    }

    [Fact]
    public void Render_Completed_UsesSuccessColor()
    {
        var cut = RenderComponent<LifecycleBadge>(p => p.Add(c => c.Stage, LifecycleStage.Completed));

        cut.Markup.Should().Contain("mud-chip-color-success");
    }

    [Fact]
    public void Render_Orphan_UsesWarningColor()
    {
        var cut = RenderComponent<LifecycleBadge>(p => p.Add(c => c.Stage, LifecycleStage.Orphan));

        cut.Markup.Should().Contain("mud-chip-color-warning");
    }

    [Fact]
    public void Renders_VikunjaProject_AsArrowChip_WhenSet()
    {
        var cut = RenderComponent<LifecycleBadge>(parameters => parameters
            .Add(p => p.Stage, LifecycleStage.Classified)
            .Add(p => p.VikunjaProject, "Zitate"));

        cut.Markup.Should().Contain("→ Zitate");
    }

    [Fact]
    public void Does_Not_Render_Arrow_When_VikunjaProject_Is_Null()
    {
        var cut = RenderComponent<LifecycleBadge>(parameters => parameters
            .Add(p => p.Stage, LifecycleStage.Classified));

        cut.Markup.Should().NotContain("→");
    }

    [Fact]
    public void Render_UnknownStage_FallsBackToQuestionMarkLabel()
    {
        // `_ => ("?", Color.Default)` arm is unreachable via valid enum values;
        // an out-of-range cast exercises the defensive default.
        var cut = RenderComponent<LifecycleBadge>(p => p.Add(c => c.Stage, (LifecycleStage)99));

        cut.Markup.Should().Contain(">?<");
    }
}

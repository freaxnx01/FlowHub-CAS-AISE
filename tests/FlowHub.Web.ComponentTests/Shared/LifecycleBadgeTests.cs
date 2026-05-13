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
}

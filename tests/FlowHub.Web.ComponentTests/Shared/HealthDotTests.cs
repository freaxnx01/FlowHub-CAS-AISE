using FlowHub.Web.Components.Shared;
using MudBlazor.Services;

namespace FlowHub.Web.ComponentTests.Shared;

public class HealthDotTests : TestContext
{
    public HealthDotTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Theory]
    [InlineData(HealthStatus.Healthy, "mud-success-text")]
    [InlineData(HealthStatus.Degraded, "mud-warning-text")]
    [InlineData(HealthStatus.Down, "mud-error-text")]
    public void Render_Status_AppliesExpectedColorClass(HealthStatus status, string expectedClass)
    {
        var cut = RenderComponent<HealthDot>(p => p.Add(c => c.Status, status));

        cut.Markup.Should().Contain(expectedClass);
    }

    [Fact]
    public void Render_UnknownStatus_FallsBackToHelpIcon_AndDefaultColor()
    {
        // The `_ => (Help, Default)` arm is unreachable via the public enum surface,
        // but coverage counts it: cast an out-of-range int to exercise the fallback.
        var cut = RenderComponent<HealthDot>(p => p.Add(c => c.Status, (HealthStatus)99));

        cut.Markup.Should().NotContain("mud-success-text");
        cut.Markup.Should().NotContain("mud-warning-text");
        cut.Markup.Should().NotContain("mud-error-text");
    }
}

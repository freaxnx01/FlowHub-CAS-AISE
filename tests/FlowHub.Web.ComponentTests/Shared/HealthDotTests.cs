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
}

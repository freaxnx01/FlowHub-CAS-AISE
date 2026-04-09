using FlowHub.Web.Components.DashboardCards;
using MudBlazor.Services;

namespace FlowHub.Web.ComponentTests.DashboardCards;

public class SkillHealthCardTests : TestContext
{
    public SkillHealthCardTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void Render_Loading_ShowsSkeletons_WhenSkillsIsNull()
    {
        var cut = RenderComponent<SkillHealthCard>(p => p.Add(c => c.Skills, null));

        cut.FindAll(".mud-skeleton").Should().NotBeEmpty();
    }

    [Fact]
    public void Render_Empty_ShowsNoSkillsMessage()
    {
        var cut = RenderComponent<SkillHealthCard>(p =>
            p.Add(c => c.Skills, Array.Empty<SkillHealth>()));

        cut.Markup.Should().Contain("No skills configured yet");
    }

    [Fact]
    public void Render_WithData_ListsEverySkillByName()
    {
        var skills = new SkillHealth[]
        {
            new("Books",  HealthStatus.Healthy,  42),
            new("Movies", HealthStatus.Healthy,   8),
            new("Quotes", HealthStatus.Degraded,  2),
        };

        var cut = RenderComponent<SkillHealthCard>(p => p.Add(c => c.Skills, skills));

        cut.Markup.Should().Contain("Books");
        cut.Markup.Should().Contain("Movies");
        cut.Markup.Should().Contain("Quotes");
        cut.Markup.Should().Contain("degraded");
    }
}

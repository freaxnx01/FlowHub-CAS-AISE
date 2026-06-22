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
            new("Zitate", HealthStatus.Degraded,  2),
        };

        var cut = RenderComponent<SkillHealthCard>(p => p.Add(c => c.Skills, skills));

        cut.Markup.Should().Contain("Books");
        cut.Markup.Should().Contain("Movies");
        cut.Markup.Should().Contain("Zitate");
        cut.Markup.Should().Contain("degraded");
    }

    [Fact]
    public void Manage_ClickInvokesCallback()
    {
        var clicks = 0;
        var cut = RenderComponent<SkillHealthCard>(p => p
            .Add(c => c.Skills, Array.Empty<SkillHealth>())
            .Add(c => c.OnManageClick, EventCallback.Factory.Create(this, () => clicks++)));

        var button = cut.FindAll("button")
            .FirstOrDefault(b => b.TextContent.Contains("Manage", StringComparison.OrdinalIgnoreCase));
        button.Should().NotBeNull();
        button!.Click();

        clicks.Should().Be(1);
    }
}

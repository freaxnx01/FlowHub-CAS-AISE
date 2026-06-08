using FlowHub.Web.Components.Layout;
using MudBlazor;
using MudBlazor.Services;

namespace FlowHub.Web.ComponentTests.Layout;

public class DemoBannerTests : TestContext
{
    public DemoBannerTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
        RenderComponent<MudPopoverProvider>();
    }

    [Fact]
    public void WithRepoUrl_RendersClickableSourceLink_OpeningNewTab()
    {
        var cut = RenderComponent<DemoBanner>(p => p
            .Add(c => c.BannerText, "Public demo — try it")
            .Add(c => c.RepoUrl, "https://github.com/freaxnx01/FlowHub-CAS-AISE"));

        cut.Markup.Should().Contain("Public demo — try it");
        var link = cut.Find("a[href='https://github.com/freaxnx01/FlowHub-CAS-AISE']");
        link.GetAttribute("target").Should().Be("_blank");
        link.TextContent.Should().Contain("Source on GitHub");
    }

    [Fact]
    public void WithoutRepoUrl_RendersNoLink()
    {
        var cut = RenderComponent<DemoBanner>(p => p.Add(c => c.BannerText, "Public demo"));

        cut.FindAll("a").Should().BeEmpty();
    }
}

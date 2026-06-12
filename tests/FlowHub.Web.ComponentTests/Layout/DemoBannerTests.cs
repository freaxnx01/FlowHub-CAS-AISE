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
        link.TextContent.Should().Contain("Source");
    }

    [Fact]
    public void WithoutRepoUrl_RendersNoLink()
    {
        var cut = RenderComponent<DemoBanner>(p => p.Add(c => c.BannerText, "Public demo"));

        cut.FindAll("a").Should().BeEmpty();
    }

    [Fact]
    public void WithSkillBoardUrl_RendersClickableVikunjaLink_OpeningNewTab()
    {
        var cut = RenderComponent<DemoBanner>(p => p
            .Add(c => c.BannerText, "Public demo — try it")
            .Add(c => c.SkillBoardUrl, "https://vikunja.demo.flowhub.freaxnx01.ch/share/abc123/auth"));

        var link = cut.Find("a[href='https://vikunja.demo.flowhub.freaxnx01.ch/share/abc123/auth']");
        link.GetAttribute("target").Should().Be("_blank");
        link.TextContent.Should().Contain("Vikunja");
    }

    [Fact]
    public void WithoutSkillBoardUrl_RendersNoVikunjaLink()
    {
        var cut = RenderComponent<DemoBanner>(p => p
            .Add(c => c.BannerText, "Public demo")
            .Add(c => c.RepoUrl, "https://github.com/freaxnx01/FlowHub-CAS-AISE"));

        cut.Markup.Should().NotContain("Vikunja");
    }

    [Fact]
    public void WithZitateBoardUrl_RendersClickableZitateLink_OpeningNewTab()
    {
        var cut = RenderComponent<DemoBanner>(p => p
            .Add(c => c.BannerText, "Public demo")
            .Add(c => c.ZitateBoardUrl, "https://vikunja.demo.flowhub.freaxnx01.ch/share/zit123/auth"));

        var link = cut.Find("a[href='https://vikunja.demo.flowhub.freaxnx01.ch/share/zit123/auth']");
        link.GetAttribute("target").Should().Be("_blank");
        link.TextContent.Should().Contain("Zitate");
    }

    [Fact]
    public void WithWallabagUrl_RendersClickableWallabagLink_OpeningNewTab()
    {
        var cut = RenderComponent<DemoBanner>(p => p
            .Add(c => c.BannerText, "Public demo")
            .Add(c => c.WallabagUrl, "https://wallabag.demo.flowhub.freaxnx01.ch"));

        var link = cut.Find("a[href='https://wallabag.demo.flowhub.freaxnx01.ch']");
        link.GetAttribute("target").Should().Be("_blank");
        link.TextContent.Should().Contain("Wallabag");
    }

    [Fact]
    public void WithPaperlessUrl_RendersClickablePaperlessLink_OpeningNewTab()
    {
        var cut = RenderComponent<DemoBanner>(p => p
            .Add(c => c.BannerText, "Public demo")
            .Add(c => c.PaperlessUrl, "https://paperless.demo.flowhub.freaxnx01.ch"));

        var link = cut.Find("a[href='https://paperless.demo.flowhub.freaxnx01.ch']");
        link.GetAttribute("target").Should().Be("_blank");
        link.TextContent.Should().Contain("paperless");
    }

    [Fact]
    public void WithServiceLogin_AndAServiceLink_RendersLoginHint()
    {
        var cut = RenderComponent<DemoBanner>(p => p
            .Add(c => c.BannerText, "Public demo")
            .Add(c => c.WallabagUrl, "https://wallabag.demo.flowhub.freaxnx01.ch")
            .Add(c => c.ServiceLogin, "flowhub / flowhub-demo"));

        cut.Markup.Should().Contain("flowhub / flowhub-demo");
    }

    [Fact]
    public void WithoutWallabagOrPaperlessUrls_RendersNeitherLink()
    {
        var cut = RenderComponent<DemoBanner>(p => p
            .Add(c => c.BannerText, "Public demo")
            .Add(c => c.ServiceLogin, "flowhub / flowhub-demo"));

        cut.Markup.Should().NotContain("Wallabag");
        cut.Markup.Should().NotContain("paperless");
        // Login hint only shows when a login-gated service link is present.
        cut.Markup.Should().NotContain("flowhub / flowhub-demo");
    }

    [Fact]
    public void WithWalkthroughUrl_RendersClickableWalkthroughLink_OpeningNewTab()
    {
        var cut = RenderComponent<DemoBanner>(p => p
            .Add(c => c.BannerText, "Public demo")
            .Add(c => c.WalkthroughUrl, "https://github.com/freaxnx01/FlowHub-CAS-AISE#explainer-videos"));

        var link = cut.Find("a[href='https://github.com/freaxnx01/FlowHub-CAS-AISE#explainer-videos']");
        link.GetAttribute("target").Should().Be("_blank");
        link.TextContent.Should().Contain("Walkthrough");
    }

    [Fact]
    public void WithoutWalkthroughUrl_RendersNoWalkthroughLink()
    {
        var cut = RenderComponent<DemoBanner>(p => p
            .Add(c => c.BannerText, "Public demo")
            .Add(c => c.RepoUrl, "https://github.com/freaxnx01/FlowHub-CAS-AISE"));

        cut.Markup.Should().NotContain("Walkthrough");
    }
}

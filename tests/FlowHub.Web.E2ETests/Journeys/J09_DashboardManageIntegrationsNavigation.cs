using System.Text.RegularExpressions;

namespace FlowHub.Web.E2ETests.Journeys;

[Trait("Category", "E2E")]
[Trait("Journey", "J09")]
[Collection("Playwright")]
public sealed class J09_DashboardManageIntegrationsNavigation : JourneyTestBase
{
    public J09_DashboardManageIntegrationsNavigation(PlaywrightFixture fixture) : base(fixture, "J09.json") { }

    [Fact]
    public async Task ClickManageIntegrations_NavigatesToIntegrationsPage()
    {
        await GotoAsync("/");

        var manage = Page.GetByRole(AriaRole.Button, new() { Name = "Manage integrations" }).First;
        await manage.WaitForAsync(new LocatorWaitForOptions { Timeout = 15_000 });
        await manage.ClickAsync();

        await Page.WaitForURLAsync(new Regex(@"/integrations$"), new PageWaitForURLOptions { Timeout = 15_000 });
    }
}

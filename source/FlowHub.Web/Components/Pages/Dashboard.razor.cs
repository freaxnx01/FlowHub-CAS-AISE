using FlowHub.Core.Captures;
using FlowHub.Core.Health;
using Microsoft.AspNetCore.Components;

namespace FlowHub.Web.Components.Pages;

public partial class Dashboard : ComponentBase
{
    [Inject] private ICaptureService CaptureService { get; set; } = default!;

    [Inject] private ISkillRegistry SkillRegistry { get; set; } = default!;

    [Inject] private IIntegrationHealthService IntegrationHealthService { get; set; } = default!;

    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private IReadOnlyList<Capture>? _recent;
    private FailureCounts? _failureCounts;
    private IReadOnlyList<SkillHealth>? _skills;
    private IReadOnlyList<IntegrationHealth>? _integrations;
    private string? _loadError;

    protected override Task OnInitializedAsync() => LoadAsync();

    private async Task LoadAsync()
    {
        _loadError = null;
        _recent = null;
        _failureCounts = null;
        _skills = null;
        _integrations = null;
        StateHasChanged();

        try
        {
            var recentTask = CaptureService.GetRecentAsync(10);
            var countsTask = CaptureService.GetFailureCountsAsync();
            var skillsTask = SkillRegistry.GetHealthAsync();
            var integrationsTask = IntegrationHealthService.GetHealthAsync();

            await Task.WhenAll(recentTask, countsTask, skillsTask, integrationsTask);

            _recent = recentTask.Result;
            _failureCounts = countsTask.Result;
            _skills = skillsTask.Result;
            _integrations = integrationsTask.Result;
        }
        catch (Exception ex)
        {
            _loadError = ex.Message;
        }
    }

    private void NavigateToOrphans() => Navigation.NavigateTo("/captures?lc=orphan");

    private void NavigateToUnhandled() => Navigation.NavigateTo("/captures?lc=unhandled");

    private void NavigateToCapture(Guid id) => Navigation.NavigateTo($"/captures/{id}");

    private void NavigateToAllCaptures() => Navigation.NavigateTo("/captures");

    private void NavigateToSkills() => Navigation.NavigateTo("/skills");

    private void NavigateToIntegrations() => Navigation.NavigateTo("/integrations");
}

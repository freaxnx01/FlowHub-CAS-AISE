using FlowHub.Core.Health;
using Microsoft.AspNetCore.Components;

namespace FlowHub.Web.Components.Pages;

public partial class Skills : ComponentBase
{
    [Inject] private ISkillRegistry SkillRegistry { get; set; } = default!;

    private IReadOnlyList<SkillHealth>? _skills;
    private string? _loadError;

    protected override Task OnInitializedAsync() => LoadAsync();

    private async Task LoadAsync()
    {
        _loadError = null;
        _skills = null;
        StateHasChanged();

        try
        {
            _skills = await SkillRegistry.GetHealthAsync();
        }
        catch (Exception ex)
        {
            _loadError = ex.Message;
        }
    }
}

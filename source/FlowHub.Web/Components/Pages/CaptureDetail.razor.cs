using FlowHub.Core.Captures;
using FlowHub.Core.Health;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FlowHub.Web.Components.Pages;

public partial class CaptureDetail : ComponentBase
{
    [Parameter] public Guid Id { get; set; }

    [Inject] private ICaptureService CaptureService { get; set; } = default!;

    [Inject] private ISkillRegistry SkillRegistry { get; set; } = default!;

    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private Capture? _capture;
    private IReadOnlyList<SkillHealth>? _skills;
    private bool _isLoading = true;
    private string? _loadError;

    protected override Task OnInitializedAsync() => LoadAsync();

    private async Task LoadAsync()
    {
        _isLoading = true;
        _loadError = null;
        _capture = null;
        StateHasChanged();

        try
        {
            // Sequential awaits — both services share the same scoped EF DbContext
            // (since the Beta MVP wired EfCaptureService + EF ISkillRegistry), and
            // EF Core forbids concurrent operations on a single context instance.
            _capture = await CaptureService.GetByIdAsync(Id);
            _skills = await SkillRegistry.GetHealthAsync();
        }
        catch (Exception ex)
        {
            _loadError = ex.Message;
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void StubAction()
    {
        Snackbar.Add(
            "This action will work once backend Skills are wired in Block 3.",
            Severity.Info);
    }

    private static string FormatRelative(DateTimeOffset when)
    {
        var delta = DateTimeOffset.UtcNow - when;
        if (delta.TotalMinutes < 1) return "now";
        if (delta.TotalMinutes < 60) return $"{(int)delta.TotalMinutes} m ago";
        if (delta.TotalHours < 24) return $"{(int)delta.TotalHours} h ago";
        return $"{(int)delta.TotalDays} d ago";
    }
}

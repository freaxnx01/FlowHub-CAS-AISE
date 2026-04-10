using FlowHub.Core.Captures;
using FlowHub.Core.Health;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FlowHub.Web.Components.Pages;

public partial class NewCapture : ComponentBase
{
    [Inject] private ICaptureService CaptureService { get; set; } = default!;

    [Inject] private ISkillRegistry SkillRegistry { get; set; } = default!;

    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private MudForm _form = default!;
    private string? _content;
    private string _selectedSkill = string.Empty;
    private IReadOnlyList<SkillHealth>? _skills;
    private bool _isSubmitting;
    private string? _skillsLoadError;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _skills = await SkillRegistry.GetHealthAsync();
        }
        catch (Exception ex)
        {
            _skillsLoadError = ex.Message;
        }
    }

    private async Task SubmitAsync()
    {
        await _form.Validate();
        if (!_form.IsValid)
        {
            return;
        }

        _isSubmitting = true;
        try
        {
            var capture = await CaptureService.SubmitAsync(_content!, ChannelKind.Web);

            var preview = capture.Content.Length > 40
                ? string.Concat(capture.Content.AsSpan(0, 37), "...")
                : capture.Content;
            Snackbar.Add($"Captured \u2713 — \"{preview}\"", Severity.Success);

            _content = null;
            _selectedSkill = string.Empty;
            await _form.ResetAsync();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Capture failed: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isSubmitting = false;
        }
    }

    private void Cancel() => Navigation.NavigateTo("/");
}

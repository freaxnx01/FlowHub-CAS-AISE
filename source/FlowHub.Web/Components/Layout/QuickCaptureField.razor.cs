using FlowHub.Core.Captures;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace FlowHub.Web.Components.Layout;

/// <summary>
/// AppBar quick-capture entry — visible on every Page (per ADR 0001 §4).
/// Submits a Capture via the <see cref="ChannelKind.Web"/> channel and
/// reports success/failure via <see cref="ISnackbar"/>. This is the only
/// component allowed to mutate server state and call snackbar directly,
/// because it has no parent Page that can sensibly own it.
/// </summary>
public partial class QuickCaptureField : ComponentBase
{
    [Inject] private ICaptureService CaptureService { get; set; } = default!;

    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private string? _input;
    private bool _isSubmitting;

    private async Task OnKeyDownAsync(KeyboardEventArgs args)
    {
        if (args.Key is "Enter" or "NumpadEnter")
        {
            await SubmitAsync();
        }
    }

    private async Task SubmitAsync()
    {
        var content = _input?.Trim();
        if (string.IsNullOrWhiteSpace(content))
        {
            Snackbar.Add("Type something first", Severity.Info);
            return;
        }

        _isSubmitting = true;
        try
        {
            var capture = await CaptureService.SubmitAsync(content, ChannelKind.Web);
            Snackbar.Add(
                $"Captured ✓ — open",
                Severity.Success,
                config => config.Action = "Open",
                key: capture.Id.ToString());
            _input = string.Empty;
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
}

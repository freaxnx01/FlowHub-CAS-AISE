using FlowHub.Core.Captures;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace FlowHub.Web.Components.Layout;

/// <summary>
/// AppBar quick-capture entry — visible on every Page (per ADR 0001 §4).
/// Submits a Capture via the <see cref="ChannelKind.Web"/> channel and
/// reports success/failure via <see cref="ISnackbar"/>. This is the only
/// component allowed to mutate server state and call snackbar directly,
/// because it has no parent Page that can sensibly own it.
/// Supports text input and file attachment (guarded by <see cref="IUploadPolicy"/>).
/// </summary>
public partial class QuickCaptureField : ComponentBase
{
    [Inject] private ICaptureService CaptureService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private IUploadPolicy UploadPolicy { get; set; } = default!;

    /// <summary>Set by <c>MainLayout</c> from <c>Demo:Mode</c> — shows one-click example prompts.</summary>
    [Parameter] public bool DemoMode { get; set; }

    /// <summary>Demo example prompts (label shown on the chip, content submitted).</summary>
    private static readonly (string Label, string Content)[] Examples =
    [
        ("🎬 The Matrix is a great movie", "The Matrix is a great movie"),
        ("✅ todo: buy milk", "todo: buy milk"),
        ("🔗 example URL", "https://en.wikipedia.org/wiki/Personal_knowledge_management"),
    ];

    private string? _input;
    private bool _isSubmitting;
    private IBrowserFile? _stagedFile;
    private string? _fileError;

    private void OnFileSelected(InputFileChangeEventArgs args)
    {
        var file = args.File;
        _fileError = ValidateFile(file);
        _stagedFile = file;
    }

    private string? ValidateFile(IBrowserFile file)
    {
        if (file.Size > UploadPolicy.MaxBytes)
            return $"File too large ({FormatBytes(file.Size)} > {FormatBytes(UploadPolicy.MaxBytes)})";
        if (!UploadPolicy.AllowedContentTypes.Contains(file.ContentType))
            return $"Type {file.ContentType} not allowed";
        return null;
    }

    private void ClearFile()
    {
        _stagedFile = null;
        _fileError = null;
    }

    private async Task OnKeyDownAsync(KeyboardEventArgs args)
    {
        if (args.Key is "Enter" or "NumpadEnter") await SubmitAsync();
    }

    private async Task SubmitAsync()
    {
        if (_stagedFile is not null)
        {
            if (_fileError is not null) return;
            await SubmitFileAsync(_stagedFile);
            return;
        }

        var content = _input?.Trim();
        if (string.IsNullOrWhiteSpace(content))
        {
            Snackbar.Add("Type something first", Severity.Info);
            return;
        }
        await SubmitTextAsync(content);
    }

    private Task SubmitExampleAsync(string content) => SubmitTextAsync(content);

    private async Task SubmitTextAsync(string content)
    {
        _isSubmitting = true;
        try
        {
            var capture = await CaptureService.SubmitAsync(content, ChannelKind.Web);
            Snackbar.Add("Captured ✓", Severity.Success, key: capture.Id.ToString());
            _input = string.Empty;
        }
        catch (Exception ex) { Snackbar.Add($"Capture failed: {ex.Message}", Severity.Error); }
        finally { _isSubmitting = false; }
    }

    private async Task SubmitFileAsync(IBrowserFile file)
    {
        _isSubmitting = true;
        try
        {
            await using var stream = file.OpenReadStream(UploadPolicy.MaxBytes);
            var input = new AttachmentInput
            {
                Content = stream,
                FileName = file.Name,
                ContentType = file.ContentType,
                SizeBytes = file.Size,
            };
            var capture = await CaptureService.SubmitAsync(content: null, ChannelKind.Web, input);
            Snackbar.Add($"Uploaded ✓ — {capture.Content}", Severity.Success, key: capture.Id.ToString());
            ClearFile();
        }
        catch (Exception ex) { Snackbar.Add($"Upload failed: {ex.Message}", Severity.Error); }
        finally { _isSubmitting = false; }
    }

    private static string FormatBytes(long bytes) =>
        bytes < 1024 ? $"{bytes} B"
        : bytes < 1024 * 1024 ? $"{bytes / 1024.0:F1} KB"
        : $"{bytes / 1024.0 / 1024.0:F2} MB";
}

using FlowHub.Core.Captures;
using FlowHub.Core.Health;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace FlowHub.Web.Components.Pages;

public partial class NewCapture : ComponentBase
{
    [Inject] private ICaptureService CaptureService { get; set; } = default!;

    [Inject] private ISkillRegistry SkillRegistry { get; set; } = default!;

    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    [Inject] private NavigationManager Navigation { get; set; } = default!;

    [Inject] private IUploadPolicy UploadPolicy { get; set; } = default!;

    private MudForm _form = default!;
    private string? _content;
    private const string AutoSkill = "__auto__";
    private string _selectedSkill = AutoSkill;
    private IReadOnlyList<SkillHealth>? _skills;
    private bool _isSubmitting;
    private string? _skillsLoadError;
    private IBrowserFile? _stagedFile;
    private string? _fileError;

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

    private void OnFileSelected(InputFileChangeEventArgs args)
    {
        var file = args.File;
        if (file.Size > UploadPolicy.MaxBytes)
        {
            _fileError = $"File too large (max {UploadPolicy.MaxBytes / 1024 / 1024} MB)";
            _stagedFile = null;
            return;
        }
        if (!UploadPolicy.AllowedContentTypes.Contains(file.ContentType))
        {
            _fileError = $"Type {file.ContentType} not allowed";
            _stagedFile = null;
            return;
        }
        _fileError = null;
        _stagedFile = file;
    }

    private async Task SubmitAsync()
    {
        if (_stagedFile is null)
        {
            await _form.Validate();
            if (!_form.IsValid)
            {
                return;
            }
        }
        else if (_fileError is not null)
        {
            return;
        }

        _isSubmitting = true;
        try
        {
            Capture capture;
            if (_stagedFile is not null)
            {
                await using var stream = _stagedFile.OpenReadStream(UploadPolicy.MaxBytes);
                capture = await CaptureService.SubmitAsync(
                    content: null, ChannelKind.Web,
                    new AttachmentInput
                    {
                        Content = stream,
                        FileName = _stagedFile.Name,
                        ContentType = _stagedFile.ContentType,
                        SizeBytes = _stagedFile.Size,
                    });
            }
            else
            {
                capture = await CaptureService.SubmitAsync(_content!, ChannelKind.Web);
            }

            var preview = capture.Content.Length > 40
                ? string.Concat(capture.Content.AsSpan(0, 37), "...")
                : capture.Content;
            Snackbar.Add($"Captured ✓ — \"{preview}\"", Severity.Success);

            _content = null;
            _stagedFile = null;
            _selectedSkill = AutoSkill;
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

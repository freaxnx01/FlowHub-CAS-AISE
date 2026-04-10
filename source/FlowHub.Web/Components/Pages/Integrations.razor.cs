using FlowHub.Core.Health;
using Microsoft.AspNetCore.Components;

namespace FlowHub.Web.Components.Pages;

public partial class Integrations : ComponentBase
{
    [Inject] private IIntegrationHealthService IntegrationHealthService { get; set; } = default!;

    private IReadOnlyList<IntegrationHealth>? _integrations;
    private string? _loadError;

    protected override Task OnInitializedAsync() => LoadAsync();

    private async Task LoadAsync()
    {
        _loadError = null;
        _integrations = null;
        StateHasChanged();

        try
        {
            _integrations = await IntegrationHealthService.GetHealthAsync();
        }
        catch (Exception ex)
        {
            _loadError = ex.Message;
        }
    }

    private static string FormatLastWrite(IntegrationHealth h)
    {
        if (h.LastWriteAt is null)
        {
            return "—";
        }

        var delta = DateTimeOffset.UtcNow - h.LastWriteAt.Value;
        var when = delta.TotalMinutes < 1
            ? "now"
            : delta.TotalMinutes < 60
                ? $"{(int)delta.TotalMinutes}m ago"
                : delta.TotalHours < 24
                    ? $"{(int)delta.TotalHours}h ago"
                    : $"{(int)delta.TotalDays}d ago";

        return h.LastWriteDuration is { } d && d.TotalMilliseconds > 1000
            ? $"{when} ({d.TotalSeconds:F1}s)"
            : when;
    }
}

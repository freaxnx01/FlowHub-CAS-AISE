using FlowHub.Core.Health;
using Microsoft.AspNetCore.Components;

namespace FlowHub.Web.Components.DashboardCards;

public partial class IntegrationHealthCard : ComponentBase
{
    [Parameter] public IReadOnlyList<IntegrationHealth>? Integrations { get; set; }

    [Parameter] public EventCallback OnManageClick { get; set; }

    private Task OnManageClickInternal() => OnManageClick.InvokeAsync();

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

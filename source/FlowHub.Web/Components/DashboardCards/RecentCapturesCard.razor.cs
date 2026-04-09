using FlowHub.Core.Captures;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FlowHub.Web.Components.DashboardCards;

public partial class RecentCapturesCard : ComponentBase
{
    [Parameter] public IReadOnlyList<Capture>? Captures { get; set; }

    [Parameter] public EventCallback<Guid> OnRowClick { get; set; }

    [Parameter] public EventCallback OnViewAllClick { get; set; }

    private Task OnRowClickInternal(DataGridRowClickEventArgs<Capture> args)
        => OnRowClick.InvokeAsync(args.Item.Id);

    private Task OnViewAllClickInternal() => OnViewAllClick.InvokeAsync();

    private static string FormatRelative(DateTimeOffset when)
    {
        var delta = DateTimeOffset.UtcNow - when;
        if (delta.TotalMinutes < 1) return "now";
        if (delta.TotalMinutes < 60) return $"{(int)delta.TotalMinutes} m";
        if (delta.TotalHours < 24) return $"{(int)delta.TotalHours} h";
        return $"{(int)delta.TotalDays} d";
    }
}

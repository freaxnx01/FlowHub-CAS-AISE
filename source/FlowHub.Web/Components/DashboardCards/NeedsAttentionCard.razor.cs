using FlowHub.Core.Captures;
using Microsoft.AspNetCore.Components;

namespace FlowHub.Web.Components.DashboardCards;

public partial class NeedsAttentionCard : ComponentBase
{
    [Parameter] public FailureCounts? Counts { get; set; }

    [Parameter] public EventCallback OnOrphanClick { get; set; }

    [Parameter] public EventCallback OnUnhandledClick { get; set; }

    private Task OnOrphanClickInternal() => OnOrphanClick.InvokeAsync();

    private Task OnUnhandledClickInternal() => OnUnhandledClick.InvokeAsync();
}

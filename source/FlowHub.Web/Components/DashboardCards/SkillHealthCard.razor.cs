using FlowHub.Core.Health;
using Microsoft.AspNetCore.Components;

namespace FlowHub.Web.Components.DashboardCards;

public partial class SkillHealthCard : ComponentBase
{
    [Parameter] public IReadOnlyList<SkillHealth>? Skills { get; set; }

    [Parameter] public EventCallback OnManageClick { get; set; }

    private Task OnManageClickInternal() => OnManageClick.InvokeAsync();
}

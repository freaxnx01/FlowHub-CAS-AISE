namespace FlowHub.Persistence.Entities;

internal sealed class SkillEntity
{
    public string Name { get; set; } = "";
    public string Status { get; set; } = "";
    public int RoutedToday { get; set; }
    public DateTimeOffset? LastResetAt { get; set; }
}

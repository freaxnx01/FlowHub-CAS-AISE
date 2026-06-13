namespace FlowHub.Web.Demo;

/// <summary>Bound from <c>Demo:Trace</c>. Gates the classification trace panel on the capture detail page.</summary>
public sealed class DemoTraceOptions
{
    public const string SectionName = "Demo:Trace";
    public bool Enabled { get; set; }
}

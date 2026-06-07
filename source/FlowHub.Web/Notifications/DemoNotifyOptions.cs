namespace FlowHub.Web.Notifications;

/// <summary>
/// Demo-only operator notifications: when configured, each freshly created Capture
/// is published to an ntfy.sh topic. Inactive unless both <see cref="BaseUrl"/> and
/// <see cref="Topic"/> are set — so the normal app and the agent-dev trial (no config)
/// never call out. The ntfy → Telegram (or any) delivery is configured at the ntfy
/// layer, not here, keeping FlowHub transport-agnostic.
/// </summary>
public sealed class DemoNotifyOptions
{
    public const string SectionName = "Demo:Notify:Ntfy";

    /// <summary>ntfy server base URL, e.g. <c>https://ntfy.freaxnx01.ch</c>.</summary>
    public string? BaseUrl { get; set; }

    /// <summary>ntfy topic, e.g. <c>flowhub-demo-captures</c>.</summary>
    public string? Topic { get; set; }

    /// <summary>Optional ntfy access token (Bearer) for a protected topic. Secret — via env.</summary>
    public string? AccessToken { get; set; }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(BaseUrl) && !string.IsNullOrWhiteSpace(Topic);
}

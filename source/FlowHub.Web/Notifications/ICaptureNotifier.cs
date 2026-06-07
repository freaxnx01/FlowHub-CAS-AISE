using FlowHub.Core.Events;

namespace FlowHub.Web.Notifications;

/// <summary>Publishes an operator notification when a Capture is created (demo-only).</summary>
public interface ICaptureNotifier
{
    Task NotifyCaptureCreatedAsync(CaptureCreated capture, CancellationToken cancellationToken);
}

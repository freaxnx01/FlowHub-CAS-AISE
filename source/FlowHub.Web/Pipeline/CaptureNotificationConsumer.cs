using FlowHub.Core.Events;
using FlowHub.Web.Notifications;
using MassTransit;

namespace FlowHub.Web.Pipeline;

/// <summary>
/// Demo-only side-effect: announces each created Capture to the configured notifier
/// (ntfy.sh). Registered only when <see cref="DemoNotifyOptions.IsConfigured"/> — see
/// Program.cs — so it has no effect in the normal app.
/// </summary>
public sealed class CaptureNotificationConsumer : IConsumer<CaptureCreated>
{
    private readonly ICaptureNotifier _notifier;

    public CaptureNotificationConsumer(ICaptureNotifier notifier) => _notifier = notifier;

    public Task Consume(ConsumeContext<CaptureCreated> context) =>
        _notifier.NotifyCaptureCreatedAsync(context.Message, context.CancellationToken);
}

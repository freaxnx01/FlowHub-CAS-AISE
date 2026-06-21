using FlowHub.Core.Captures;
using FlowHub.Core.Events;
using MassTransit;

namespace FlowHub.Web.Pipeline;

/// <summary>
/// Terminal fault handler for the async pipeline (ADR 0003): consumes the
/// <c>Fault&lt;T&gt;</c> messages MassTransit publishes when a consumer exhausts its
/// retries, and maps each to the correct failure stage with a <c>FailureReason</c> —
/// <c>Fault&lt;CaptureCreated&gt;</c> (enrichment) → <c>MarkOrphanAsync</c>,
/// <c>Fault&lt;CaptureClassified&gt;</c> (routing) → <c>MarkUnhandledAsync</c>. It does
/// not retry the fault itself (that would loop); recovery is via the manual retry endpoint.
/// </summary>
public sealed partial class LifecycleFaultObserver
    : IConsumer<Fault<CaptureCreated>>, IConsumer<Fault<CaptureClassified>>
{
    private readonly ICaptureService _captureService;
    private readonly ILogger<LifecycleFaultObserver> _logger;

    public LifecycleFaultObserver(
        ICaptureService captureService,
        ILogger<LifecycleFaultObserver> logger)
    {
        _captureService = captureService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<Fault<CaptureCreated>> context)
    {
        var captureId = context.Message.Message.CaptureId;
        var reason = FormatReason(context.Message);

        try
        {
            await _captureService.MarkOrphanAsync(captureId, reason, context.CancellationToken);
        }
        // D5 (best-effort, no recursive retry): swallow any MarkXxx failure so the message
        // doesn't re-fault into Fault<Fault<T>>. Cancellation must still propagate.
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogObserverFailed(ex, captureId, "Orphan");
        }
    }

    public async Task Consume(ConsumeContext<Fault<CaptureClassified>> context)
    {
        var captureId = context.Message.Message.CaptureId;
        var reason = FormatReason(context.Message);

        try
        {
            await _captureService.MarkUnhandledAsync(captureId, reason, context.CancellationToken);
        }
        // D5 (best-effort, no recursive retry): swallow any MarkXxx failure so the message
        // doesn't re-fault into Fault<Fault<T>>. Cancellation must still propagate.
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogObserverFailed(ex, captureId, "Unhandled");
        }
    }

    private static string FormatReason<T>(Fault<T> fault) where T : class
    {
        var first = fault.Exceptions?.FirstOrDefault();
        return first is null
            ? "exhausted retries: <no exception detail>"
            : $"exhausted retries: {first.ExceptionType}: {first.Message}";
    }

    [LoggerMessage(EventId = 1003, Level = LogLevel.Error,
        Message = "LifecycleFaultObserver failed to mark capture {CaptureId} as {TargetStage}")]
    private partial void LogObserverFailed(Exception ex, Guid captureId, string targetStage);
}

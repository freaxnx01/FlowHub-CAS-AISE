namespace FlowHub.Core.Captures;

public sealed record CaptureFilter(
    IReadOnlyList<LifecycleStage>? Stages,
    ChannelKind? Source,
    int Limit,
    CaptureCursor? Cursor);

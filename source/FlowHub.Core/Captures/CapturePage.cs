namespace FlowHub.Core.Captures;

public sealed record CapturePage(
    IReadOnlyList<Capture> Items,
    CaptureCursor? Next);

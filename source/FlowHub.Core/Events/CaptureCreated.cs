using FlowHub.Core.Captures;

namespace FlowHub.Core.Events;

public sealed record CaptureCreated(
    Guid CaptureId,
    string Content,
    ChannelKind Source,
    DateTimeOffset CreatedAt);

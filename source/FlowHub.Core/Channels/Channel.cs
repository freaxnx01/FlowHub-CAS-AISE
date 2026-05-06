using FlowHub.Core.Captures;
using FlowHub.Core.Health;

namespace FlowHub.Core.Channels;

public sealed record Channel(
    string Name,
    ChannelKind Kind,
    bool IsEnabled,
    HealthStatus Status,
    DateTimeOffset? LastActiveAt);

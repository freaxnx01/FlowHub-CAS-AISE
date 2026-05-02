using FlowHub.Core.Captures;

namespace FlowHub.Api.Requests;

public sealed record CreateCaptureRequest(string Content, ChannelKind Source);

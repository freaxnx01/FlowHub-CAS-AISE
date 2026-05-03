namespace FlowHub.AI;

/// <summary>
/// Captured at <see cref="AiServiceCollectionExtensions.AddFlowHubAi"/> time and consumed
/// by <see cref="AiBootLogger"/> to write the 3020/3021 startup log line.
/// </summary>
public sealed record AiRegistrationOutcome(
    bool UsesAi,
    AiProvider? Provider,
    string? Model,
    string Reason); // "configured" | "no-provider" | "missing-key"

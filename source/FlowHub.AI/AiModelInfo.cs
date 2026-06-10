namespace FlowHub.AI;

/// <summary>Active AI provider + model, injected into <see cref="AiClassifier"/> for trace reporting.</summary>
public sealed record AiModelInfo(string Provider, string Model);

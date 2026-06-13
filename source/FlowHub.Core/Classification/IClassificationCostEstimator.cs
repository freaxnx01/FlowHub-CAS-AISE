namespace FlowHub.Core.Classification;

/// <summary>Estimates the USD cost of a classification call from its token counts.</summary>
public interface IClassificationCostEstimator
{
    /// <returns>Estimated cost in USD, or null when the model is unknown or tokens are unavailable.</returns>
    decimal? Estimate(string? model, int? promptTokens, int? completionTokens);
}

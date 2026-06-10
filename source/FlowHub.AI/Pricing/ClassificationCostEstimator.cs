using FlowHub.Core.Classification;
using Microsoft.Extensions.Options;

namespace FlowHub.AI.Pricing;

public sealed class ClassificationCostEstimator : IClassificationCostEstimator
{
    // Built-in: the public demo's free model. All other models are priced via config.
    // MUST stay in sync with the demo's Ai__OpenRouter__Model default in
    // demo/docker-compose.yml — if they diverge, the demo cost shows "—" instead of "free".
    private const string FreeDemoModel = "google/gemma-4-31b-it:free";

    private readonly Dictionary<string, ModelPrice> _prices;

    public ClassificationCostEstimator(IOptions<ClassificationPricingOptions> options)
    {
        var map = new Dictionary<string, ModelPrice>(StringComparer.Ordinal)
        {
            [FreeDemoModel] = new ModelPrice { Model = FreeDemoModel, Input = 0m, Output = 0m },
        };
        foreach (var price in options.Value.Models)
        {
            if (!string.IsNullOrWhiteSpace(price.Model))
            {
                map[price.Model] = price; // config overrides built-in
            }
        }
        _prices = map;
    }

    public decimal? Estimate(string? model, int? promptTokens, int? completionTokens)
    {
        if (model is null || promptTokens is null || completionTokens is null)
        {
            return null;
        }
        if (!_prices.TryGetValue(model, out var price))
        {
            return null;
        }
        return (promptTokens.Value * price.Input / 1_000_000m)
             + (completionTokens.Value * price.Output / 1_000_000m);
    }
}

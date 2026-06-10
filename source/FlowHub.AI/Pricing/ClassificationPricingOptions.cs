namespace FlowHub.AI.Pricing;

/// <summary>Bound from <c>Ai:Pricing</c>. Prices are USD per 1,000,000 tokens.</summary>
public sealed class ClassificationPricingOptions
{
    public const string SectionName = "Ai:Pricing";
    public List<ModelPrice> Models { get; set; } = [];
}

public sealed class ModelPrice
{
    public string Model { get; set; } = "";
    public decimal Input { get; set; }
    public decimal Output { get; set; }
}

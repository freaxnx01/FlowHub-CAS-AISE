using FlowHub.AI.Pricing;
using FlowHub.Core.Classification;
using Microsoft.Extensions.Options;

namespace FlowHub.Web.ComponentTests.Pricing;

public sealed class ClassificationCostEstimatorTests
{
    private static ClassificationCostEstimator Build(params ModelPrice[] configured) =>
        new(Options.Create(new ClassificationPricingOptions { Models = [.. configured] }));

    [Fact]
    public void Estimate_ConfiguredModel_ComputesFromPerMillionRates()
    {
        var sut = Build(new ModelPrice { Model = "m", Input = 3m, Output = 15m }); // $/Mtok
        // 1000 * 3/1e6 + 500 * 15/1e6 = 0.003 + 0.0075 = 0.0105
        sut.Estimate("m", 1000, 500).Should().Be(0.0105m);
    }

    [Fact]
    public void Estimate_FreeDemoModel_IsZero()
    {
        var sut = Build();
        sut.Estimate("google/gemma-4-31b-it:free", 1000, 500).Should().Be(0m);
    }

    [Fact]
    public void Estimate_UnknownModel_ReturnsNull()
    {
        var sut = Build();
        sut.Estimate("mystery-model", 1000, 500).Should().BeNull();
    }

    [Fact]
    public void Estimate_NullModelOrTokens_ReturnsNull()
    {
        var sut = Build(new ModelPrice { Model = "m", Input = 1m, Output = 1m });
        sut.Estimate(null, 10, 10).Should().BeNull();
        sut.Estimate("m", null, 10).Should().BeNull();
        sut.Estimate("m", 10, null).Should().BeNull();
    }

    [Fact]
    public void Estimate_ConfigOverridesBuiltInFreeModel()
    {
        var sut = Build(new ModelPrice { Model = "google/gemma-4-31b-it:free", Input = 2m, Output = 4m });
        sut.Estimate("google/gemma-4-31b-it:free", 1_000_000, 1_000_000).Should().Be(6m);
    }
}

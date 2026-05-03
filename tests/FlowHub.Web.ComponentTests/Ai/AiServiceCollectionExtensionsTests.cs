using FlowHub.AI;
using FlowHub.Core.Classification;
using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlowHub.Web.ComponentTests.Ai;

public sealed class AiServiceCollectionExtensionsTests
{
    private static ServiceProvider Build(Dictionary<string, string?> settings)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFlowHubAi(config);
        return services.BuildServiceProvider();
    }

    [Fact]
    public void AddFlowHubAi_NoProviderConfigured_RegistersKeywordClassifier()
    {
        var sp = Build(new());

        sp.GetRequiredService<IClassifier>().Should().BeOfType<KeywordClassifier>();
        sp.GetRequiredService<AiRegistrationOutcome>().UsesAi.Should().BeFalse();
        sp.GetRequiredService<AiRegistrationOutcome>().Reason.Should().Be("no-provider");
    }

    [Fact]
    public void AddFlowHubAi_AnthropicProviderWithoutKey_RegistersKeywordClassifier()
    {
        var sp = Build(new() { ["Ai:Provider"] = "Anthropic" });

        sp.GetRequiredService<IClassifier>().Should().BeOfType<KeywordClassifier>();
        sp.GetRequiredService<AiRegistrationOutcome>().UsesAi.Should().BeFalse();
        sp.GetRequiredService<AiRegistrationOutcome>().Reason.Should().Be("missing-key");
    }

    [Fact]
    public void AddFlowHubAi_OpenRouterProviderWithoutKey_RegistersKeywordClassifier()
    {
        var sp = Build(new() { ["Ai:Provider"] = "OpenRouter" });

        sp.GetRequiredService<IClassifier>().Should().BeOfType<KeywordClassifier>();
        sp.GetRequiredService<AiRegistrationOutcome>().UsesAi.Should().BeFalse();
    }

    [Fact]
    public void AddFlowHubAi_AnthropicWithKey_RegistersAiClassifier()
    {
        var sp = Build(new()
        {
            ["Ai:Provider"] = "Anthropic",
            ["Ai:Anthropic:ApiKey"] = "sk-ant-test",
        });

        sp.GetRequiredService<IClassifier>().Should().BeOfType<AiClassifier>();
        sp.GetRequiredService<KeywordClassifier>().Should().NotBeNull(); // floor still resolvable
        var outcome = sp.GetRequiredService<AiRegistrationOutcome>();
        outcome.UsesAi.Should().BeTrue();
        outcome.Provider.Should().Be(AiProvider.Anthropic);
        outcome.Model.Should().Be("claude-haiku-4-5-20251001");
    }

    [Fact]
    public void AddFlowHubAi_OpenRouterWithKey_RegistersAiClassifier()
    {
        var sp = Build(new()
        {
            ["Ai:Provider"] = "OpenRouter",
            ["Ai:OpenRouter:ApiKey"] = "or-test",
        });

        sp.GetRequiredService<IClassifier>().Should().BeOfType<AiClassifier>();
        var outcome = sp.GetRequiredService<AiRegistrationOutcome>();
        outcome.UsesAi.Should().BeTrue();
        outcome.Provider.Should().Be(AiProvider.OpenRouter);
        outcome.Model.Should().Be("meta-llama/llama-3.1-70b-instruct");
    }

    [Fact]
    public void AddFlowHubAi_InvalidProviderValue_ThrowsInvalidOperationException()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Ai:Provider"] = "Bogus" })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();

        var act = () => services.AddFlowHubAi(config);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*Bogus*");
    }

    [Fact]
    public void AddFlowHubAi_ProviderCaseInsensitive_RegistersAi()
    {
        var sp = Build(new()
        {
            ["Ai:Provider"] = "anthropic",
            ["Ai:Anthropic:ApiKey"] = "sk-ant-test",
        });

        sp.GetRequiredService<IClassifier>().Should().BeOfType<AiClassifier>();
    }

    [Fact]
    public void AddFlowHubAi_ModelOverride_RespectsConfig()
    {
        var sp = Build(new()
        {
            ["Ai:Provider"] = "Anthropic",
            ["Ai:Anthropic:ApiKey"] = "sk-ant-test",
            ["Ai:Anthropic:Model"] = "claude-sonnet-4-7",
        });

        sp.GetRequiredService<AiRegistrationOutcome>().Model.Should().Be("claude-sonnet-4-7");
    }
}

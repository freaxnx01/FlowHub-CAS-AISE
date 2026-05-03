using Anthropic.SDK;
using FlowHub.Core.Classification;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenAI;
using System.ClientModel;

namespace FlowHub.AI;

public static class AiServiceCollectionExtensions
{
    private const string DefaultAnthropicModel = "claude-haiku-4-5-20251001";
    private const string DefaultOpenRouterModel = "meta-llama/llama-3.1-70b-instruct";
    private const string DefaultOpenRouterEndpoint = "https://openrouter.ai/api/v1";

    public static IServiceCollection AddFlowHubAi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<KeywordClassifier>();

        var outcome = ResolveOutcome(configuration);
        services.AddSingleton(outcome);
        services.AddHostedService<AiBootLogger>();

        if (!outcome.UsesAi)
        {
            services.AddSingleton<IClassifier>(sp => sp.GetRequiredService<KeywordClassifier>());
            return services;
        }

        var apiKey = configuration[$"Ai:{outcome.Provider}:ApiKey"]!;
        var model = outcome.Model!;
        var maxTokens = int.TryParse(configuration["Ai:MaxOutputTokens"], out var parsed) ? parsed : 300;

        services.AddSingleton<IChatClient>(sp =>
            BuildChatClient(outcome.Provider!.Value, apiKey, model, configuration)
                .AsBuilder()
                .UseOpenTelemetry(
                    loggerFactory: sp.GetService<ILoggerFactory>(),
                    sourceName: null,
                    configure: null)
                .Build());

        services.AddSingleton(sp => new AiClassifier(
            sp.GetRequiredService<IChatClient>(),
            sp.GetRequiredService<KeywordClassifier>(),
            sp.GetRequiredService<ILogger<AiClassifier>>(),
            new ChatOptions { MaxOutputTokens = maxTokens, Temperature = 0.2f }));
        services.AddSingleton<IClassifier>(sp => sp.GetRequiredService<AiClassifier>());

        return services;
    }

    private static AiRegistrationOutcome ResolveOutcome(IConfiguration configuration)
    {
        var raw = configuration["Ai:Provider"];

        if (string.IsNullOrWhiteSpace(raw))
        {
            return new AiRegistrationOutcome(UsesAi: false, Provider: null, Model: null, Reason: "no-provider");
        }

        if (!Enum.TryParse<AiProvider>(raw, ignoreCase: true, out var provider))
        {
            throw new InvalidOperationException(
                $"Invalid Ai__Provider value: '{raw}'. Expected 'Anthropic' or 'OpenRouter'.");
        }

        var apiKey = configuration[$"Ai:{provider}:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new AiRegistrationOutcome(UsesAi: false, Provider: provider, Model: null, Reason: "missing-key");
        }

        var model = configuration[$"Ai:{provider}:Model"]
            ?? DefaultModelFor(provider);
        return new AiRegistrationOutcome(UsesAi: true, Provider: provider, Model: model, Reason: "configured");
    }

    private static string DefaultModelFor(AiProvider provider) => provider switch
    {
        AiProvider.Anthropic => DefaultAnthropicModel,
        AiProvider.OpenRouter => DefaultOpenRouterModel,
        _ => throw new ArgumentOutOfRangeException(nameof(provider)),
    };

    private static IChatClient BuildChatClient(
        AiProvider provider,
        string apiKey,
        string model,
        IConfiguration configuration)
    {
        switch (provider)
        {
            case AiProvider.Anthropic:
                // MessagesEndpoint directly implements IChatClient in Anthropic.SDK 5.x.
                // The model is conveyed per-request via ChatOptions.ModelId, which we
                // set as a default via ConfigureOptions so callers need not repeat it.
                return new AnthropicClient(apiKey).Messages
                    .AsBuilder()
                    .ConfigureOptions(o => o.ModelId = model)
                    .Build();

            case AiProvider.OpenRouter:
                var endpoint = new Uri(configuration["Ai:OpenRouter:Endpoint"] ?? DefaultOpenRouterEndpoint);
                var openAiOptions = new OpenAIClientOptions { Endpoint = endpoint };
                return new OpenAIClient(new ApiKeyCredential(apiKey), openAiOptions)
                    .GetChatClient(model)
                    .AsIChatClient();

            default:
                throw new ArgumentOutOfRangeException(nameof(provider));
        }
    }
}

using System.ClientModel;
using FlowHub.AI.Classification.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;

namespace FlowHub.AI.Classification.Services;

public class OpenRouterClientFactory
{
    private readonly string _apiKey;
    private readonly Uri _baseUrl;

    public OpenRouterClientFactory(IConfiguration config)
    {
        _apiKey = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY")
                  ?? config["OpenRouter:ApiKey"]
                  ?? throw new InvalidOperationException(
                      "Set OPENROUTER_API_KEY env variable or OpenRouter:ApiKey in appsettings.json");

        _baseUrl = new Uri(config["OpenRouter:BaseUrl"] ?? "https://openrouter.ai/api/v1");
    }

    public IChatClient CreateClient(ModelConfig model)
    {
        var client = new OpenAIClient(
            new ApiKeyCredential(_apiKey),
            new OpenAIClientOptions { Endpoint = _baseUrl });

        return client.GetChatClient(model.Id).AsIChatClient();
    }
}

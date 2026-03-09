using System.Diagnostics;
using System.Text.Json;
using FlowHub.AI.Classification.Config;
using FlowHub.AI.Classification.Models;
using Microsoft.Extensions.AI;

namespace FlowHub.AI.Classification.Services;

public class ClassificationService
{
    private static readonly string SystemPrompt = $$"""
        You are a message classifier for FlowHub, a personal inbox system.
        Classify the user's message into exactly one of these skills:

        {{SkillCatalog.BuildSkillListPrompt()}}

        Respond ONLY with valid JSON, no markdown fences, no extra text:
        {"skill": "<skill name>", "confidence": <0.0-1.0>, "reasoning": "<1-2 sentences why>"}
        """;

    private readonly OpenRouterClientFactory _clientFactory;
    private readonly List<ModelConfig> _models;
    private static readonly TimeSpan ModelTimeout = TimeSpan.FromSeconds(30);

    public ClassificationService(OpenRouterClientFactory clientFactory, List<ModelConfig> models)
    {
        _clientFactory = clientFactory;
        _models = models;
    }

    public async Task<List<ClassificationResult>> ClassifyAsync(string message)
    {
        var tasks = _models.Select(model => ClassifyWithModelAsync(model, message));
        var results = await Task.WhenAll(tasks);
        return results.ToList();
    }

    private async Task<ClassificationResult> ClassifyWithModelAsync(ModelConfig model, string message)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            var client = _clientFactory.CreateClient(model);
            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, SystemPrompt),
                new(ChatRole.User, message),
            };

            using var cts = new CancellationTokenSource(ModelTimeout);
            var response = await client.GetResponseAsync(messages, cancellationToken: cts.Token);
            sw.Stop();

            var json = response.Text?.Trim() ?? "";

            // Strip markdown code fences if LLM wraps the JSON
            if (json.StartsWith("```"))
            {
                var lines = json.Split('\n');
                json = string.Join('\n', lines.Skip(1).TakeWhile(l => !l.StartsWith("```")));
            }

            var result = JsonSerializer.Deserialize<ClassificationResult>(json);

            return result is null
                ? ErrorResult(model, sw.Elapsed, "Failed to deserialize JSON response")
                : result with { ModelName = model.DisplayName, Latency = sw.Elapsed };
        }
        catch (TaskCanceledException)
        {
            sw.Stop();
            return ErrorResult(model, sw.Elapsed, "Timeout (30s)");
        }
        catch (Exception ex)
        {
            sw.Stop();
            return ErrorResult(model, sw.Elapsed, ex.Message);
        }
    }

    private static ClassificationResult ErrorResult(ModelConfig model, TimeSpan latency, string error) =>
        new()
        {
            ModelName = model.DisplayName,
            Latency = latency,
            Error = error,
        };
}

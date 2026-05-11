using System.Diagnostics;
using FlowHub.AI;
using FlowHub.Core.Captures;
using FlowHub.Core.Classification;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FlowHub.AiPing;

internal static class Program
{
    private static readonly string[] ConfigEnvKeys =
    [
        "Ai__Provider",
        "Ai__Anthropic__ApiKey",
        "Ai__Anthropic__Model",
        "Ai__OpenRouter__ApiKey",
        "Ai__OpenRouter__Model",
        "Ai__OpenRouter__Endpoint",
        "Ai__MaxOutputTokens",
        "Ai__TimeoutSeconds",
        "Embeddings__ApiKey",
        "Embeddings__BaseUrl",
        "Embeddings__Model",
        "Embeddings__Dimensions",
        "Embeddings__TimeoutSeconds",
    ];

    public static async Task<int> Main(string[] args)
    {
        var command = args.Length > 0 ? args[0] : "ping";
        var text = args.Length > 1 ? string.Join(' ', args.Skip(1)) : null;

        var config = BuildConfig();
        var provider = config["Ai:Provider"] ?? "(unset)";
        var anthropicModel = config["Ai:Anthropic:Model"];
        var openRouterModel = config["Ai:OpenRouter:Model"];
        var embeddingsModel = config["Embeddings:Model"];

        Console.WriteLine($"FlowHub AiPing — command={command}");
        Console.WriteLine($"  Ai:Provider              = {provider}");
        Console.WriteLine($"  Ai:Anthropic:Model       = {anthropicModel ?? "(default)"}");
        Console.WriteLine($"  Ai:OpenRouter:Model      = {openRouterModel ?? "(default)"}");
        Console.WriteLine($"  Embeddings:Model         = {embeddingsModel ?? "(default)"}");
        Console.WriteLine();

        try
        {
            return command switch
            {
                "ping" => await RunPingAsync(config),
                "classify" => await RunClassifyAsync(config, text),
                "embed" => await RunEmbedAsync(config, text),
                _ => Usage(),
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"FAIL: {ex.GetType().Name}: {ex.Message}");
            if (ex.InnerException is { } inner)
            {
                Console.Error.WriteLine($"  inner: {inner.GetType().Name}: {inner.Message}");
            }
            return 1;
        }
    }

    private static IConfiguration BuildConfig()
    {
        var dict = new Dictionary<string, string?>();
        foreach (var key in ConfigEnvKeys)
        {
            var value = Environment.GetEnvironmentVariable(key);
            if (!string.IsNullOrWhiteSpace(value))
            {
                dict[key.Replace("__", ":", StringComparison.Ordinal)] = value;
            }
        }
        return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
    }

    private static ServiceProvider BuildClassifierProvider(IConfiguration config)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddFlowHubAi(config);
        return services.BuildServiceProvider();
    }

    private static ServiceProvider BuildEmbeddingProvider(IConfiguration config)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddFlowHubEmbeddings(config);
        return services.BuildServiceProvider();
    }

    private static async Task<int> RunPingAsync(IConfiguration config)
    {
        await using (var sp = BuildClassifierProvider(config))
        {
            var outcome = sp.GetRequiredService<AiRegistrationOutcome>();

            if (!outcome.UsesAi)
            {
                Console.Error.WriteLine($"FAIL: AI provider not configured (reason={outcome.Reason}).");
                Console.Error.WriteLine("      Set Ai__Provider and the matching Ai__<Provider>__ApiKey.");
                return 2;
            }

            var chat = sp.GetRequiredService<IChatClient>();
            Console.WriteLine($"Pinging chat: {outcome.Provider} ({outcome.Model})…");

            var sw = Stopwatch.StartNew();
            var response = await chat.GetResponseAsync(
                "Reply with the single word: OK",
                new ChatOptions { MaxOutputTokens = 16, Temperature = 0f });
            sw.Stop();

            var reply = response.Text?.Trim() ?? string.Empty;
            Console.WriteLine($"  reply    = {Truncate(reply, 200)}");
            Console.WriteLine($"  latency  = {sw.ElapsedMilliseconds} ms");
        }

        Console.WriteLine();

        await using (var sp = BuildEmbeddingProvider(config))
        {
            var embedding = sp.GetService<IEmbeddingService>();
            if (embedding is null)
            {
                Console.WriteLine("Pinging embeddings: skipped (Embeddings__ApiKey not set)");
            }
            else
            {
                var model = config["Embeddings:Model"] ?? "mistral-embed";
                Console.WriteLine($"Pinging embeddings: {model}…");

                var sw = Stopwatch.StartNew();
                var vector = await embedding.GenerateAsync("ping", CancellationToken.None);
                sw.Stop();

                if (vector is null)
                {
                    Console.Error.WriteLine("  FAIL: embedding generation returned null (provider error — see logs).");
                    return 1;
                }

                Console.WriteLine($"  dimensions = {vector.Length}");
                Console.WriteLine($"  latency    = {sw.ElapsedMilliseconds} ms");
            }
        }

        Console.WriteLine();
        Console.WriteLine("OK");
        return 0;
    }

    private static async Task<int> RunClassifyAsync(IConfiguration config, string? text)
    {
        await using var sp = BuildClassifierProvider(config);
        var outcome = sp.GetRequiredService<AiRegistrationOutcome>();

        if (!outcome.UsesAi)
        {
            Console.Error.WriteLine($"FAIL: AI provider not configured (reason={outcome.Reason}).");
            return 2;
        }

        var classifier = sp.GetRequiredService<IClassifier>();
        var samples = text is { Length: > 0 }
            ? new[] { text }
            : new[]
            {
                "https://en.wikipedia.org/wiki/Hexagonal_architecture",
                "todo: buy milk on Saturday",
            };

        foreach (var sample in samples)
        {
            Console.WriteLine($"Input: {Truncate(sample, 120)}");
            var sw = Stopwatch.StartNew();
            var result = await classifier.ClassifyAsync(sample, CancellationToken.None);
            sw.Stop();
            Console.WriteLine($"  MatchedSkill = {result.MatchedSkill}");
            Console.WriteLine($"  Title        = {result.Title}");
            Console.WriteLine($"  Tags         = [{string.Join(", ", result.Tags ?? [])}]");
            Console.WriteLine($"  latency      = {sw.ElapsedMilliseconds} ms");
            Console.WriteLine();
        }
        return 0;
    }

    private static async Task<int> RunEmbedAsync(IConfiguration config, string? text)
    {
        await using var sp = BuildEmbeddingProvider(config);
        var embedding = sp.GetService<IEmbeddingService>();
        if (embedding is null)
        {
            Console.Error.WriteLine("FAIL: Embeddings not configured. Set Embeddings__ApiKey (and optionally Embeddings__BaseUrl, Embeddings__Model).");
            return 2;
        }

        var input = text is { Length: > 0 }
            ? text
            : "FlowHub captures small bits of information and routes them to the right skill.";

        Console.WriteLine($"Input: {Truncate(input, 120)}");
        var sw = Stopwatch.StartNew();
        var vector = await embedding.GenerateAsync(input, CancellationToken.None);
        sw.Stop();

        if (vector is null)
        {
            Console.Error.WriteLine("FAIL: embedding generation returned null (provider error — see logs).");
            return 1;
        }

        var preview = string.Join(", ", vector.Take(8).Select(v => v.ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture)));
        Console.WriteLine($"  dimensions = {vector.Length}");
        Console.WriteLine($"  preview    = [{preview}, …]");
        Console.WriteLine($"  latency    = {sw.ElapsedMilliseconds} ms");
        Console.WriteLine("OK");
        return 0;
    }

    private static int Usage()
    {
        Console.Error.WriteLine("usage: FlowHub.AiPing <ping|classify|embed> [text…]");
        return 64;
    }

    private static string Truncate(string value, int max)
        => value.Length <= max ? value : value[..max] + "…";
}

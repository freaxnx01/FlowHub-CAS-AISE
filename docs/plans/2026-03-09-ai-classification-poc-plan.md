# AI Classification PoC Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Build a standalone console app that classifies messages into FlowHub skills using multiple LLMs via OpenRouter, comparing results side-by-side.

**Architecture:** Console app using M.E.AI `IChatClient` with OpenAI-compatible provider pointing at OpenRouter. Parallel `Task.WhenAll` calls to 3 free-tier LLMs. Interactive + demo mode.

**Tech Stack:** .NET 10, Microsoft.Extensions.AI.OpenAI, OpenAI .NET SDK, System.Text.Json

**Design Doc:** `docs/plans/2026-03-09-ai-classification-poc-design.md`

---

### Task 1: Scaffold the PoC project

**Files:**
- Create: `poc/FlowHub.AI.Classification/FlowHub.AI.Classification.csproj`
- Create: `poc/FlowHub.AI.Classification/appsettings.json`

**Step 1: Create project directory**

Run: `mkdir -p poc/FlowHub.AI.Classification`

**Step 2: Create the .csproj**

Create `poc/FlowHub.AI.Classification/FlowHub.AI.Classification.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.AI.OpenAI" Version="*" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="*" />
    <PackageReference Include="Microsoft.Extensions.Configuration.EnvironmentVariables" Version="*" />
  </ItemGroup>

  <ItemGroup>
    <Content Include="appsettings.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </Content>
  </ItemGroup>

</Project>
```

**Step 3: Create appsettings.json**

Create `poc/FlowHub.AI.Classification/appsettings.json`:

```json
{
  "OpenRouter": {
    "ApiKey": "YOUR_KEY_HERE",
    "BaseUrl": "https://openrouter.ai/api/v1"
  },
  "Models": [
    { "Id": "google/gemma-3-4b-it:free", "DisplayName": "Gemma 3 4B" },
    { "Id": "meta-llama/llama-4-scout:free", "DisplayName": "Llama 4 Scout" },
    { "Id": "qwen/qwen3-4b:free", "DisplayName": "Qwen3 4B" }
  ]
}
```

**Step 4: Add appsettings.Development.json to .gitignore**

Append to root `.gitignore`:
```
appsettings.Development.json
```

**Step 5: Verify project restores**

Run: `dotnet restore poc/FlowHub.AI.Classification/FlowHub.AI.Classification.csproj`
Expected: Restore succeeds

**Step 6: Commit**

```bash
git add poc/FlowHub.AI.Classification/FlowHub.AI.Classification.csproj poc/FlowHub.AI.Classification/appsettings.json .gitignore
git commit -m "feat(poc): scaffold AI classification PoC project"
```

---

### Task 2: Create Models

**Files:**
- Create: `poc/FlowHub.AI.Classification/Models/SkillDefinition.cs`
- Create: `poc/FlowHub.AI.Classification/Models/ClassificationResult.cs`
- Create: `poc/FlowHub.AI.Classification/Models/ModelConfig.cs`

**Step 1: Create Models directory**

Run: `mkdir -p poc/FlowHub.AI.Classification/Models`

**Step 2: Create SkillDefinition.cs**

```csharp
namespace FlowHub.AI.Classification.Models;

public record SkillDefinition(string Name, string Description);
```

**Step 3: Create ClassificationResult.cs**

```csharp
using System.Text.Json.Serialization;

namespace FlowHub.AI.Classification.Models;

public record ClassificationResult
{
    [JsonPropertyName("skill")]
    public string Skill { get; init; } = string.Empty;

    [JsonPropertyName("confidence")]
    public double Confidence { get; init; }

    [JsonPropertyName("reasoning")]
    public string Reasoning { get; init; } = string.Empty;

    // Not from JSON — set by ClassificationService
    [JsonIgnore]
    public string ModelName { get; init; } = string.Empty;

    [JsonIgnore]
    public TimeSpan Latency { get; init; }

    [JsonIgnore]
    public string? Error { get; init; }

    public bool IsError => Error is not null;
}
```

**Step 4: Create ModelConfig.cs**

```csharp
namespace FlowHub.AI.Classification.Models;

public record ModelConfig
{
    public string Id { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
}
```

**Step 5: Verify build**

Run: `dotnet build poc/FlowHub.AI.Classification/FlowHub.AI.Classification.csproj`
Expected: Build succeeds (with warning about no Program.cs entry point — that's fine)

**Step 6: Commit**

```bash
git add poc/FlowHub.AI.Classification/Models/
git commit -m "feat(poc): add classification models"
```

---

### Task 3: Create SkillCatalog and ModelCatalog

**Files:**
- Create: `poc/FlowHub.AI.Classification/Config/SkillCatalog.cs`
- Create: `poc/FlowHub.AI.Classification/Config/ModelCatalog.cs`

**Step 1: Create Config directory**

Run: `mkdir -p poc/FlowHub.AI.Classification/Config`

**Step 2: Create SkillCatalog.cs**

```csharp
using FlowHub.AI.Classification.Models;

namespace FlowHub.AI.Classification.Config;

public static class SkillCatalog
{
    public static IReadOnlyList<SkillDefinition> Skills { get; } =
    [
        new("ArticleSkill", "News articles, blog posts, tech articles, read-later content"),
        new("HomelabSkill", "Self-hosted software, homelab services, server tools, Docker images"),
        new("BookSkill", "Books, e-books, audiobooks, reading recommendations"),
        new("MovieSkill", "Movies, TV series, streaming content, watchlists"),
        new("DocumentSkill", "PDFs, scanned documents, receipts, photos of paperwork"),
        new("KnowledgeSkill", "Facts, explanations, how-things-work, learning topics"),
        new("QuoteSkill", "Quotes, sayings, citations, memorable phrases"),
        new("GenericSkill", "Anything that doesn't fit the above categories"),
    ];

    public static string BuildSkillListPrompt()
    {
        return string.Join('\n', Skills.Select((s, i) =>
            $"{i + 1}. {s.Name} — {s.Description}"));
    }
}
```

**Step 3: Create ModelCatalog.cs**

```csharp
using FlowHub.AI.Classification.Models;
using Microsoft.Extensions.Configuration;

namespace FlowHub.AI.Classification.Config;

public static class ModelCatalog
{
    public static List<ModelConfig> LoadFromConfig(IConfiguration config)
    {
        var models = new List<ModelConfig>();
        config.GetSection("Models").Bind(models);
        return models;
    }
}
```

**Step 4: Verify build**

Run: `dotnet build poc/FlowHub.AI.Classification/FlowHub.AI.Classification.csproj`
Expected: Build succeeds

**Step 5: Commit**

```bash
git add poc/FlowHub.AI.Classification/Config/
git commit -m "feat(poc): add skill and model catalogs"
```

---

### Task 4: Create OpenRouterClientFactory

**Files:**
- Create: `poc/FlowHub.AI.Classification/Services/OpenRouterClientFactory.cs`

**Step 1: Create Services directory**

Run: `mkdir -p poc/FlowHub.AI.Classification/Services`

**Step 2: Create OpenRouterClientFactory.cs**

This uses the OpenAI .NET SDK with a custom endpoint (OpenRouter), then wraps it as M.E.AI `IChatClient`.

```csharp
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
```

**Step 3: Verify build**

Run: `dotnet build poc/FlowHub.AI.Classification/FlowHub.AI.Classification.csproj`
Expected: Build succeeds

**Step 4: Commit**

```bash
git add poc/FlowHub.AI.Classification/Services/OpenRouterClientFactory.cs
git commit -m "feat(poc): add OpenRouter client factory using M.E.AI"
```

---

### Task 5: Create ClassificationService

**Files:**
- Create: `poc/FlowHub.AI.Classification/Services/ClassificationService.cs`

**Step 1: Create ClassificationService.cs**

```csharp
using System.Diagnostics;
using System.Text.Json;
using FlowHub.AI.Classification.Config;
using FlowHub.AI.Classification.Models;
using Microsoft.Extensions.AI;

namespace FlowHub.AI.Classification.Services;

public class ClassificationService
{
    private static readonly string SystemPrompt = $"""
        You are a message classifier for FlowHub, a personal inbox system.
        Classify the user's message into exactly one of these skills:

        {SkillCatalog.BuildSkillListPrompt()}

        Respond ONLY with valid JSON, no markdown fences, no extra text:
        {{"skill": "<skill name>", "confidence": <0.0-1.0>, "reasoning": "<1-2 sentences why>"}}
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
```

**Step 2: Verify build**

Run: `dotnet build poc/FlowHub.AI.Classification/FlowHub.AI.Classification.csproj`
Expected: Build succeeds

**Step 3: Commit**

```bash
git add poc/FlowHub.AI.Classification/Services/ClassificationService.cs
git commit -m "feat(poc): add classification service with parallel LLM calls"
```

---

### Task 6: Create DemoMessages

**Files:**
- Create: `poc/FlowHub.AI.Classification/Demo/DemoMessages.cs`

**Step 1: Create Demo directory**

Run: `mkdir -p poc/FlowHub.AI.Classification/Demo`

**Step 2: Create DemoMessages.cs**

```csharp
namespace FlowHub.AI.Classification.Demo;

public record DemoMessage(string Text, string ExpectedSkill);

public static class DemoMessages
{
    public static IReadOnlyList<DemoMessage> All { get; } =
    [
        new("https://www.imdb.com/de/title/tt2543312/", "MovieSkill"),
        new("Muss ich unbedingt schauen!", "MovieSkill"),
        new("https://heise.de/news/some-article-12345", "ArticleSkill"),
        new("Interesting blog post about Rust memory safety", "ArticleSkill"),
        new("https://www.thalia.ch/shop/some-book", "BookSkill"),
        new("Dieses Buch muss ich unbedingt lesen", "BookSkill"),
        new("Check out Portainer for managing Docker containers", "HomelabSkill"),
        new("https://github.com/louislam/uptime-kuma", "HomelabSkill"),
        new("Photo of a receipt from the dentist", "DocumentSkill"),
        new("PDF: Steuererklaerung_2025.pdf", "DocumentSkill"),
        new("Wie funktioniert eigentlich DNS?", "KnowledgeSkill"),
        new("I want to understand how transformers work in ML", "KnowledgeSkill"),
        new("\"Be the change you wish to see in the world\"", "QuoteSkill"),
        new("Zitat: \"Der Weg ist das Ziel\"", "QuoteSkill"),
        new("Remind me to buy milk tomorrow", "GenericSkill"),
        new("Call mom on Sunday", "GenericSkill"),
    ];
}
```

**Step 3: Verify build**

Run: `dotnet build poc/FlowHub.AI.Classification/FlowHub.AI.Classification.csproj`
Expected: Build succeeds

**Step 4: Commit**

```bash
git add poc/FlowHub.AI.Classification/Demo/
git commit -m "feat(poc): add demo messages with expected skill classifications"
```

---

### Task 7: Create Program.cs — Console output and CLI

**Files:**
- Create: `poc/FlowHub.AI.Classification/Program.cs`

**Step 1: Create Program.cs**

```csharp
using FlowHub.AI.Classification.Config;
using FlowHub.AI.Classification.Demo;
using FlowHub.AI.Classification.Models;
using FlowHub.AI.Classification.Services;
using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile("appsettings.Development.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var models = ModelCatalog.LoadFromConfig(config);
var factory = new OpenRouterClientFactory(config);
var service = new ClassificationService(factory, models);

var isDemoMode = args.Contains("--demo");

Console.WriteLine("=== FlowHub AI Classification PoC ===");
Console.WriteLine($"Models: {string.Join(", ", models.Select(m => m.DisplayName))}");
Console.WriteLine();

if (isDemoMode)
{
    await RunDemoMode(service);
}
else
{
    await RunInteractiveMode(service);
}

static async Task RunDemoMode(ClassificationService service)
{
    var totalResults = new List<(DemoMessage Message, List<ClassificationResult> Results)>();

    foreach (var demo in DemoMessages.All)
    {
        Console.WriteLine($"Message: {demo.Text}");
        var results = await service.ClassifyAsync(demo.Text);
        PrintResultsTable(results, demo.ExpectedSkill);
        totalResults.Add((demo, results));
        Console.WriteLine();
    }

    PrintSummary(totalResults);
}

static async Task RunInteractiveMode(ClassificationService service)
{
    Console.WriteLine("Type a message to classify (or 'quit' to exit):");
    Console.WriteLine();

    while (true)
    {
        Console.Write("> ");
        var input = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(input) || input.Equals("quit", StringComparison.OrdinalIgnoreCase))
            break;

        var results = await service.ClassifyAsync(input);
        PrintResultsTable(results);
        Console.WriteLine();
    }
}

static void PrintResultsTable(List<ClassificationResult> results, string? expectedSkill = null)
{
    const int modelWidth = 18;
    const int skillWidth = 16;
    const int confWidth = 10;
    const int reasonWidth = 42;
    const int latencyWidth = 9;
    const int matchWidth = 5;

    var hasExpected = expectedSkill is not null;
    var header = hasExpected
        ? $"| {"Model",-modelWidth} | {"Skill",-skillWidth} | {"Conf",confWidth} | {"Reasoning",-reasonWidth} | {"Time",latencyWidth} | {"OK?",matchWidth} |"
        : $"| {"Model",-modelWidth} | {"Skill",-skillWidth} | {"Conf",confWidth} | {"Reasoning",-reasonWidth} | {"Time",latencyWidth} |";

    var separator = hasExpected
        ? $"|{new string('-', modelWidth + 2)}|{new string('-', skillWidth + 2)}|{new string('-', confWidth + 2)}|{new string('-', reasonWidth + 2)}|{new string('-', latencyWidth + 2)}|{new string('-', matchWidth + 2)}|"
        : $"|{new string('-', modelWidth + 2)}|{new string('-', skillWidth + 2)}|{new string('-', confWidth + 2)}|{new string('-', reasonWidth + 2)}|{new string('-', latencyWidth + 2)}|";

    Console.WriteLine(separator);
    Console.WriteLine(header);
    Console.WriteLine(separator);

    foreach (var r in results)
    {
        var skill = r.IsError ? "ERROR" : r.Skill;
        var conf = r.IsError ? "-" : r.Confidence.ToString("F2");
        var reason = r.IsError ? r.Error! : r.Reasoning;
        if (reason.Length > reasonWidth) reason = reason[..(reasonWidth - 3)] + "...";
        var latency = $"{r.Latency.TotalSeconds:F1}s";
        var match = hasExpected
            ? (r.IsError ? "-" : (r.Skill == expectedSkill ? "Y" : "N"))
            : null;

        var row = hasExpected
            ? $"| {r.ModelName,-modelWidth} | {skill,-skillWidth} | {conf,confWidth} | {reason,-reasonWidth} | {latency,latencyWidth} | {match,matchWidth} |"
            : $"| {r.ModelName,-modelWidth} | {skill,-skillWidth} | {conf,confWidth} | {reason,-reasonWidth} | {latency,latencyWidth} |";

        Console.WriteLine(row);
    }

    Console.WriteLine(separator);
}

static void PrintSummary(List<(DemoMessage Message, List<ClassificationResult> Results)> allResults)
{
    Console.WriteLine("=== SUMMARY ===");
    Console.WriteLine();

    var modelNames = allResults.First().Results.Select(r => r.ModelName).ToList();

    foreach (var modelName in modelNames)
    {
        var total = 0;
        var correct = 0;

        foreach (var (message, results) in allResults)
        {
            var result = results.FirstOrDefault(r => r.ModelName == modelName);
            if (result is null || result.IsError) continue;

            total++;
            if (result.Skill == message.ExpectedSkill) correct++;
        }

        var pct = total > 0 ? (correct * 100.0 / total) : 0;
        Console.WriteLine($"{modelName}: {correct}/{total} correct ({pct:F0}%)");
    }
}
```

**Step 2: Verify build**

Run: `dotnet build poc/FlowHub.AI.Classification/FlowHub.AI.Classification.csproj`
Expected: Build succeeds with no errors

**Step 3: Commit**

```bash
git add poc/FlowHub.AI.Classification/Program.cs
git commit -m "feat(poc): add Program.cs with interactive and demo CLI modes"
```

---

### Task 8: End-to-end test run

**Step 1: Set API key**

Run: `export OPENROUTER_API_KEY="your-actual-key"`

**Step 2: Run interactive mode — test single message**

Run: `dotnet run --project poc/FlowHub.AI.Classification`
Input: `https://www.imdb.com/de/title/tt2543312/`
Expected: Table with 3 model results, all classifying as MovieSkill

**Step 3: Run demo mode**

Run: `dotnet run --project poc/FlowHub.AI.Classification -- --demo`
Expected: 16 messages classified, summary showing accuracy per model

**Step 4: Verify error handling — test with invalid key**

Run: `OPENROUTER_API_KEY=invalid dotnet run --project poc/FlowHub.AI.Classification`
Expected: ERROR shown per model, app doesn't crash

**Step 5: Final commit if any fixes were needed**

```bash
git add -A poc/FlowHub.AI.Classification/
git commit -m "fix(poc): adjustments from end-to-end testing"
```

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

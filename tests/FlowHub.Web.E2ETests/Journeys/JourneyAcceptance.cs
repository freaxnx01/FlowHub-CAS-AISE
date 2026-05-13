using System.Text.Json;
using System.Text.Json.Serialization;

namespace FlowHub.Web.E2ETests.Journeys;

/// <summary>
/// Acceptance-criteria record loaded from a J{NN}.json file sitting next to a
/// Playwright spec. Mirrors the shape documented in docs/design/journeys.md so
/// the JSON and the C# spec can be reviewed side by side.
/// </summary>
public sealed record JourneyAcceptance(
    string Id,
    string Category,
    string Description,
    string EntryUrl,
    IReadOnlyList<string> Preconditions,
    IReadOnlyList<string> Steps,
    IReadOnlyList<string> Expected,
    bool Passes)
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static JourneyAcceptance Load(string jsonFileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Journeys", jsonFileName);
        using var stream = File.OpenRead(path);
        return JsonSerializer.Deserialize<JourneyAcceptance>(stream, Options)
            ?? throw new InvalidOperationException($"Could not parse {jsonFileName}");
    }
}

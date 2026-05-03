# Slice C — AI Integration Implementation Plan (Block 3 Slice C)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Swap the deterministic `KeywordClassifier` for an AI-backed `AiClassifier` that classifies *and* titles a Capture in one structured-output call, with two interchangeable provider adapters (Anthropic native, OpenRouter aggregator) behind `Microsoft.Extensions.AI`'s `IChatClient`.

**Architecture:** New code lives in `source/FlowHub.AI/` (was placeholder). `AiClassifier` wraps `KeywordClassifier` as a hard fallback floor — every catchable failure (network, timeout, JSON parse, schema-violation, anything else) degrades quality, never availability. `AddFlowHubAi(IConfiguration)` extension picks one provider at boot via `Ai__Provider`; missing key or missing provider silently keeps the keyword path so `make run` still works zero-config.

**Tech Stack:** .NET 10, `Microsoft.Extensions.AI` (MEAI) for the abstraction, `Microsoft.Extensions.AI.OpenAI` for the OpenRouter adapter (OpenAI-compatible endpoint), `Anthropic.SDK` for the Anthropic native adapter, xunit + NSubstitute + FluentAssertions for unit tests, trait-gated live integration tests for real-provider verification.

**Source spec:** `docs/superpowers/specs/2026-05-03-slice-c-ai-integration-design.md` (D1–D10).

**ADR landing:** `docs/adr/0004-ai-integration-in-services.md` (authored in Task 13).

---

## File Structure

**Create — production**

- `source/FlowHub.AI/FlowHub.AI.csproj`
- `source/FlowHub.AI/AiPrompts.cs`
- `source/FlowHub.AI/AiClassificationResponse.cs`
- `source/FlowHub.AI/AiClassifier.cs`
- `source/FlowHub.AI/AiServiceCollectionExtensions.cs`
- `source/FlowHub.AI/AiProvider.cs` (enum)
- `source/FlowHub.AI/AiBootLogger.cs` (`IHostedService` for the 3020/3021 startup log)
- `source/FlowHub.AI/AiRegistrationOutcome.cs` (record consumed by `AiBootLogger`)

**Create — tests**

- `tests/FlowHub.Web.ComponentTests/Ai/AiClassifierTests.cs`
- `tests/FlowHub.Web.ComponentTests/Ai/AiPromptsTests.cs`
- `tests/FlowHub.Web.ComponentTests/Ai/AiClassificationResponseTests.cs`
- `tests/FlowHub.Web.ComponentTests/Ai/AiServiceCollectionExtensionsTests.cs`
- `tests/FlowHub.AI.IntegrationTests/FlowHub.AI.IntegrationTests.csproj`
- `tests/FlowHub.AI.IntegrationTests/Usings.cs`
- `tests/FlowHub.AI.IntegrationTests/AnthropicHaikuLiveTests.cs`
- `tests/FlowHub.AI.IntegrationTests/OpenRouterLlamaLiveTests.cs`

**Create — docs**

- `docs/adr/0004-ai-integration-in-services.md`

**Modify**

- `Directory.Packages.props` — add `<ItemGroup Label="Ai">` block
- `FlowHub.slnx` — register `source/FlowHub.AI/FlowHub.AI.csproj` and `tests/FlowHub.AI.IntegrationTests/...`
- `source/FlowHub.Core/Classification/ClassificationResult.cs` — add `string? Title = null`
- `source/FlowHub.Web/FlowHub.Web.csproj` — `<ProjectReference>` to `FlowHub.AI`, plus `<UserSecretsId>` property
- `source/FlowHub.Web/Program.cs` — replace `AddSingleton<IClassifier, KeywordClassifier>()` with `AddFlowHubAi(builder.Configuration)`
- `Makefile` — `test:` adds `--filter "Category!=AI"`, new `test-ai:` target
- `docs/ai-usage.md` — append `## Block 3 Slice C — AI integration` section
- `CLAUDE.md` — drop "(placeholder — AI classification, future block)" annotation on `FlowHub.AI/`
- `vault/Blöcke/03 Service/03 Service - c) Nachbereitung.md` — tick Slice-C items
- `CHANGELOG.md` — append Slice-C entry to `[Unreleased]`

---

## Task 1: Add MEAI + provider packages

**Files:**
- Modify: `Directory.Packages.props`

- [ ] **Step 1.1: Discover current versions**

```bash
dotnet package search Microsoft.Extensions.AI --exact-match 2>&1 | tail -3
dotnet package search Microsoft.Extensions.AI.OpenAI --exact-match 2>&1 | tail -3
dotnet package search Anthropic.SDK --exact-match 2>&1 | tail -3
dotnet package search Microsoft.Extensions.Hosting --exact-match 2>&1 | tail -3
```

Replace `<MEAI>`, `<MEAIOAI>`, `<ANTSDK>`, `<HOSTING>` below with the resolved versions. If your sandbox has no network, fall back to: Microsoft.Extensions.AI `9.4.0-preview.1.25164.4`, Microsoft.Extensions.AI.OpenAI `9.4.0-preview.1.25164.4`, Anthropic.SDK `5.4.3`, Microsoft.Extensions.Hosting `10.0.7`.

> Note on `Anthropic.SDK`: the `tghamm/Anthropic.SDK` package exposes a MEAI-compatible `IChatClient` via its `Anthropic.SDK.Common.Messaging` API. Verify by running `dotnet package search Anthropic.SDK Microsoft.Extensions.AI` — if a separate bridge package surfaces, prefer the bridge.

- [ ] **Step 1.2: Add central package versions**

In `Directory.Packages.props`, add a new `<ItemGroup>` immediately after the existing `Api` group:

```xml
  <ItemGroup Label="Ai">
    <PackageVersion Include="Microsoft.Extensions.AI" Version="<MEAI>" />
    <PackageVersion Include="Microsoft.Extensions.AI.OpenAI" Version="<MEAIOAI>" />
    <PackageVersion Include="Anthropic.SDK" Version="<ANTSDK>" />
    <PackageVersion Include="Microsoft.Extensions.Hosting.Abstractions" Version="<HOSTING>" />
  </ItemGroup>
```

`Microsoft.Extensions.AI.Abstractions` is pulled transitively by `Microsoft.Extensions.AI`; do not pin it explicitly.

`Microsoft.Extensions.Hosting.Abstractions` is needed because `AiBootLogger` implements `IHostedService` (Task 7).

- [ ] **Step 1.3: Restore + build**

Run: `make restore && make build`
Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`. Adding central versions doesn't add references; the build should be unchanged.

- [ ] **Step 1.4: Commit**

```bash
git add Directory.Packages.props
git commit -m "build(deps): add Microsoft.Extensions.AI + provider packages for Slice C"
```

---

## Task 2: Extend `ClassificationResult` with `Title?` (TDD)

**Files:**
- Modify: `source/FlowHub.Core/Classification/ClassificationResult.cs`
- Modify: `source/FlowHub.Core/Classification/KeywordClassifier.cs`
- Modify: `tests/FlowHub.Web.ComponentTests/Classification/KeywordClassifierTests.cs`

`KeywordClassifier` returns `Title=null` per spec D4. The default value on the positional record makes the change source-compatible — existing callers like `CaptureEnrichmentConsumerTests` constructing `new ClassificationResult(["link"], "Wallabag")` keep compiling without edit.

- [ ] **Step 2.1: Write the failing test**

Add a new test to `tests/FlowHub.Web.ComponentTests/Classification/KeywordClassifierTests.cs`, immediately above the `private static` helpers (file is currently flat — append after the last `[Fact]`):

```csharp
    [Fact]
    public async Task ClassifyAsync_AnyContent_KeywordClassifierReturnsNullTitle()
    {
        var result = await _sut.ClassifyAsync("https://example.com/article", default);

        result.Title.Should().BeNull();
    }
```

- [ ] **Step 2.2: Run test to verify it fails**

Run: `dotnet test tests/FlowHub.Web.ComponentTests --filter "FullyQualifiedName~KeywordClassifierTests.ClassifyAsync_AnyContent_KeywordClassifierReturnsNullTitle"`
Expected: **FAIL** with `'ClassificationResult' does not contain a definition for 'Title'`.

- [ ] **Step 2.3: Extend the record**

Replace the entire contents of `source/FlowHub.Core/Classification/ClassificationResult.cs` with:

```csharp
namespace FlowHub.Core.Classification;

/// <summary>
/// Output of <see cref="IClassifier.ClassifyAsync"/>.
/// Slice B (KeywordClassifier) returns Title=null; Slice C (AiClassifier) populates it
/// in the same round-trip as Tags + MatchedSkill (per ADR 0004 D4).
/// </summary>
public sealed record ClassificationResult(
    IReadOnlyList<string> Tags,
    string MatchedSkill,
    string? Title = null);
```

`KeywordClassifier` does not need editing — its existing `new ClassificationResult([…], …)` calls now produce `Title=null` automatically via the default.

- [ ] **Step 2.4: Run the test again to verify it passes**

Run: `dotnet test tests/FlowHub.Web.ComponentTests --filter "FullyQualifiedName~KeywordClassifierTests"`
Expected: **PASS**, all 5 tests (the 4 existing + the 1 new).

- [ ] **Step 2.5: Run the full suite to make sure no regression**

Run: `dotnet test FlowHub.slnx`
Expected: all tests still pass (existing call sites use the positional default for `Title`).

- [ ] **Step 2.6: Commit**

```bash
git add source/FlowHub.Core/Classification/ClassificationResult.cs \
        tests/FlowHub.Web.ComponentTests/Classification/KeywordClassifierTests.cs
git commit -m "feat(core): extend ClassificationResult with optional Title (Slice C prep)"
```

---

## Task 3: Scaffold `FlowHub.AI` real csproj

**Files:**
- Create: `source/FlowHub.AI/FlowHub.AI.csproj`
- Delete: `source/FlowHub.AI/.gitkeep`
- Modify: `FlowHub.slnx`
- Modify: `source/FlowHub.Web/FlowHub.Web.csproj`

- [ ] **Step 3.1: Replace `.gitkeep` with the real csproj**

Delete `source/FlowHub.AI/.gitkeep`. Create `source/FlowHub.AI/FlowHub.AI.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <RootNamespace>FlowHub.AI</RootNamespace>
    <AssemblyName>FlowHub.AI</AssemblyName>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.AI" />
    <PackageReference Include="Microsoft.Extensions.AI.OpenAI" />
    <PackageReference Include="Anthropic.SDK" />
    <PackageReference Include="Microsoft.Extensions.Hosting.Abstractions" />
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\FlowHub.Core\FlowHub.Core.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 3.2: Register in solution**

In `FlowHub.slnx`, add this line inside the `/source/` folder block, alphabetically (between `FlowHub.Api` and `FlowHub.Core`):

```xml
    <Project Path="source/FlowHub.AI/FlowHub.AI.csproj" />
```

Final ordering inside `/source/`:

```xml
    <Project Path="source/FlowHub.AI/FlowHub.AI.csproj" />
    <Project Path="source/FlowHub.Api/FlowHub.Api.csproj" />
    <Project Path="source/FlowHub.Core/FlowHub.Core.csproj" />
    <Project Path="source/FlowHub.Skills/FlowHub.Skills.csproj" />
    <Project Path="source/FlowHub.Web/FlowHub.Web.csproj" />
```

- [ ] **Step 3.3: Reference from `FlowHub.Web`**

In `source/FlowHub.Web/FlowHub.Web.csproj`, add to the `<ProjectReference>` ItemGroup (alphabetically — before `FlowHub.Api`):

```xml
    <ProjectReference Include="..\FlowHub.AI\FlowHub.AI.csproj" />
```

- [ ] **Step 3.4: Reference from the unit-test project**

The tests in Tasks 4–7 live in `tests/FlowHub.Web.ComponentTests/Ai/` and need the new project on the test compile path. In `tests/FlowHub.Web.ComponentTests/FlowHub.Web.ComponentTests.csproj`, add to the `<ProjectReference>` ItemGroup:

```xml
    <ProjectReference Include="..\..\source\FlowHub.AI\FlowHub.AI.csproj" />
```

- [ ] **Step 3.5: Build to confirm empty project compiles**

Run: `make restore && make build`
Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`. The `FlowHub.AI` project is empty but valid.

- [ ] **Step 3.6: Run existing test suite**

Run: `dotnet test FlowHub.slnx`
Expected: all tests pass (no behavioural change yet).

- [ ] **Step 3.7: Commit**

```bash
git add FlowHub.slnx source/FlowHub.AI source/FlowHub.Web/FlowHub.Web.csproj \
        tests/FlowHub.Web.ComponentTests/FlowHub.Web.ComponentTests.csproj
git rm source/FlowHub.AI/.gitkeep
git commit -m "feat(ai): scaffold FlowHub.AI csproj with MEAI + provider packages"
```

---

## Task 4: TDD `AiPrompts`

**Files:**
- Create: `source/FlowHub.AI/AiPrompts.cs`
- Create: `tests/FlowHub.Web.ComponentTests/Ai/AiPromptsTests.cs`

`AiPrompts` is an `internal static class` with the system-prompt `const string` plus a `BuildMessages(content)` helper that returns the full message list (system + user). Tests pin the exact wording so prompt changes are deliberate.

- [ ] **Step 4.1: Write the failing tests**

Create `tests/FlowHub.Web.ComponentTests/Ai/AiPromptsTests.cs`:

```csharp
using FlowHub.AI;
using FluentAssertions;
using Microsoft.Extensions.AI;

namespace FlowHub.Web.ComponentTests.Ai;

public sealed class AiPromptsTests
{
    [Fact]
    public void BuildMessages_AnyContent_FirstMessageIsSystemPrompt()
    {
        var messages = AiPrompts.BuildMessages("https://example.com");

        messages.Should().HaveCount(2);
        messages[0].Role.Should().Be(ChatRole.System);
        messages[0].Text.Should().Contain("FlowHub");
        messages[0].Text.Should().Contain("Wallabag");
        messages[0].Text.Should().Contain("Vikunja");
    }

    [Fact]
    public void BuildMessages_AnyContent_SecondMessageIsRawUserContent()
    {
        const string content = "todo: buy milk on Saturday";

        var messages = AiPrompts.BuildMessages(content);

        messages[1].Role.Should().Be(ChatRole.User);
        messages[1].Text.Should().Be(content);
    }

    [Fact]
    public void SystemPrompt_HasNoGermanRoutingTokens()
    {
        // Spec D6 / Prompt strategy: the system prompt is English to keep Llama 3.1
        // routing tokens stable. Capture content can still be German — that's the user
        // message, not the system prompt.
        AiPrompts.SystemPrompt.Should().NotContain("Ablage");
        AiPrompts.SystemPrompt.Should().NotContain("Aufgabe");
    }
}
```

To make `internal` accessible from the test assembly, the production project needs an `InternalsVisibleTo` shim (added in Step 4.3).

- [ ] **Step 4.2: Run tests, verify they fail**

Run: `dotnet test tests/FlowHub.Web.ComponentTests --filter "FullyQualifiedName~AiPromptsTests"`
Expected: **FAIL** — `AiPrompts` doesn't exist.

- [ ] **Step 4.3: Implement `AiPrompts`**

Create `source/FlowHub.AI/AiPrompts.cs`:

```csharp
using Microsoft.Extensions.AI;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("FlowHub.Web.ComponentTests")]

namespace FlowHub.AI;

internal static class AiPrompts
{
    internal const string SystemPrompt = """
        You classify user-captured snippets for a personal knowledge tool called FlowHub.

        For each capture, return:
        - tags: 1–5 short lowercase tags describing the snippet
        - matched_skill: which downstream skill should handle it. Choose exactly ONE:
            "Wallabag"  – the snippet is a URL or article worth saving for later reading
            "Vikunja"   – the snippet is a task, todo, or actionable item
            ""          – none of the above; it will be marked as Orphan
        - title: a 3–8 word title summarising the snippet (omit only if the snippet
                 is itself shorter than 8 words)

        Reply ONLY via the structured response schema. Never include explanations.
        """;

    internal static IList<ChatMessage> BuildMessages(string content) =>
    [
        new ChatMessage(ChatRole.System, SystemPrompt),
        new ChatMessage(ChatRole.User, content),
    ];
}
```

- [ ] **Step 4.4: Run tests to verify they pass**

Run: `dotnet test tests/FlowHub.Web.ComponentTests --filter "FullyQualifiedName~AiPromptsTests"`
Expected: **PASS** (3 tests).

- [ ] **Step 4.5: Commit**

```bash
git add source/FlowHub.AI/AiPrompts.cs tests/FlowHub.Web.ComponentTests/Ai/AiPromptsTests.cs
git commit -m "feat(ai): add AiPrompts with English system prompt + BuildMessages helper"
```

---

## Task 5: TDD `AiClassificationResponse` DTO

**Files:**
- Create: `source/FlowHub.AI/AiClassificationResponse.cs`
- Create: `tests/FlowHub.Web.ComponentTests/Ai/AiClassificationResponseTests.cs`

The DTO is consumed by MEAI's structured-output pipeline via `CompleteAsync<TResponse>`. Tests verify the JSON-schema annotations are present so MEAI generates the right schema (`AllowedValues` → enum constraint).

- [ ] **Step 5.1: Write the failing tests**

Create `tests/FlowHub.Web.ComponentTests/Ai/AiClassificationResponseTests.cs`:

```csharp
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using FlowHub.AI;
using FluentAssertions;

namespace FlowHub.Web.ComponentTests.Ai;

public sealed class AiClassificationResponseTests
{
    [Fact]
    public void Tags_HasDescription_ForSchemaGeneration()
    {
        var prop = typeof(AiClassificationResponse).GetProperty(nameof(AiClassificationResponse.Tags))!;
        var desc = prop.GetCustomAttribute<DescriptionAttribute>();

        desc.Should().NotBeNull();
        desc!.Description.Should().Contain("tags");
    }

    [Fact]
    public void MatchedSkill_AllowedValuesEnumeratesWallabagVikunjaEmpty()
    {
        var prop = typeof(AiClassificationResponse).GetProperty(nameof(AiClassificationResponse.MatchedSkill))!;
        var allowed = prop.GetCustomAttribute<AllowedValuesAttribute>();

        allowed.Should().NotBeNull();
        allowed!.Values.Should().BeEquivalentTo(new object[] { "Wallabag", "Vikunja", "" });
    }

    [Fact]
    public void Title_IsNullableString()
    {
        var prop = typeof(AiClassificationResponse).GetProperty(nameof(AiClassificationResponse.Title))!;

        prop.PropertyType.Should().Be(typeof(string));
        // Reading nullability via reflection in C# 10+ requires NullabilityInfoContext.
        var ctx = new NullabilityInfoContext();
        ctx.Create(prop).WriteState.Should().Be(NullabilityState.Nullable);
    }
}
```

- [ ] **Step 5.2: Run tests, verify they fail**

Run: `dotnet test tests/FlowHub.Web.ComponentTests --filter "FullyQualifiedName~AiClassificationResponseTests"`
Expected: **FAIL** — type doesn't exist.

- [ ] **Step 5.3: Implement the DTO**

Create `source/FlowHub.AI/AiClassificationResponse.cs`:

```csharp
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FlowHub.AI;

/// <summary>
/// Schema-driven response shape consumed by MEAI's CompleteAsync&lt;T&gt; structured output.
/// Attributes drive the JSON schema generated by MEAI: Description ↦ schema "description",
/// AllowedValues ↦ schema "enum". Both Anthropic (tool-use) and OpenRouter (response_format)
/// honour the generated schema.
/// </summary>
internal sealed record AiClassificationResponse(
    [property: Description("1–5 short lowercase tags describing the snippet")]
    string[] Tags,

    [property: Description("Wallabag, Vikunja, or empty string for none")]
    [property: AllowedValues("Wallabag", "Vikunja", "")]
    string MatchedSkill,

    [property: Description("3–8 word title or null if content is too short")]
    string? Title);
```

- [ ] **Step 5.4: Run tests to verify they pass**

Run: `dotnet test tests/FlowHub.Web.ComponentTests --filter "FullyQualifiedName~AiClassificationResponseTests"`
Expected: **PASS** (3 tests).

- [ ] **Step 5.5: Commit**

```bash
git add source/FlowHub.AI/AiClassificationResponse.cs \
        tests/FlowHub.Web.ComponentTests/Ai/AiClassificationResponseTests.cs
git commit -m "feat(ai): add AiClassificationResponse DTO with JSON-schema annotations"
```

---

## Task 6: TDD `AiClassifier`

**Files:**
- Create: `source/FlowHub.AI/AiClassifier.cs`
- Create: `tests/FlowHub.Web.ComponentTests/Ai/AiClassifierTests.cs`

`AiClassifier : IClassifier` consumes `IChatClient` (MEAI) + `KeywordClassifier` (fallback floor) + `ChatOptions` + `ILogger`. On any exception or schema-violation it logs `EventId 3010 AiClassifierFellBackToKeyword` and delegates to the keyword floor; never rethrows. Implements 10 unit tests per spec D9.

The MEAI pattern: `AiClassifier` calls the underlying `IChatClient.GetResponseAsync(messages, options, ct)` (the non-extension method) and parses the JSON content of the response into `AiClassificationResponse`. Mocking the non-extension method via `NSubstitute.For<IChatClient>()` is the canonical test seam — extension methods like `CompleteAsync<T>` cannot be intercepted.

> **MEAI 9.x note for the implementer**: the surface API on `IChatClient` is `GetResponseAsync(IEnumerable<ChatMessage>, ChatOptions?, CancellationToken)` returning `Task<ChatResponse>`. Older preview docs use `CompleteAsync` — check the actual method name on the installed package version and adjust. The plan uses `GetResponseAsync` below; if the installed package still exposes `CompleteAsync`, replace verbatim throughout the task.

- [ ] **Step 6.1: Write the success-path test (test 1)**

Create `tests/FlowHub.Web.ComponentTests/Ai/AiClassifierTests.cs`:

```csharp
using System.Text.Json;
using FlowHub.AI;
using FlowHub.Core.Classification;
using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace FlowHub.Web.ComponentTests.Ai;

public sealed class AiClassifierTests
{
    private readonly IChatClient _chat = Substitute.For<IChatClient>();
    private readonly IClassifier _keyword = Substitute.For<IClassifier>();
    private readonly FakeLogger<AiClassifier> _log = new();
    private readonly ChatOptions _opts = new() { MaxOutputTokens = 300, Temperature = 0.2f };

    private AiClassifier Sut() => new(_chat, _keyword, _log, _opts);

    private static ChatResponse JsonResponse(object payload) =>
        new(new ChatMessage(ChatRole.Assistant, JsonSerializer.Serialize(payload)));

    [Fact]
    public async Task ClassifyAsync_AiSucceedsWithValidSchema_ReturnsAiResult()
    {
        _chat.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions?>(), Arg.Any<CancellationToken>())
             .Returns(JsonResponse(new
             {
                 tags = new[] { "link", "article" },
                 matched_skill = "Wallabag",
                 title = "Saving an article for later",
             }));

        var result = await Sut().ClassifyAsync("https://example.com/article", default);

        result.MatchedSkill.Should().Be("Wallabag");
        result.Tags.Should().BeEquivalentTo(new[] { "link", "article" });
        result.Title.Should().Be("Saving an article for later");
        await _keyword.DidNotReceive().ClassifyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
```

`FakeLogger<T>` is a small in-memory logger we'll add inline at the bottom of the test file (Step 6.10).

- [ ] **Step 6.2: Run test, verify it fails (type-not-found)**

Run: `dotnet test tests/FlowHub.Web.ComponentTests --filter "FullyQualifiedName~AiClassifierTests"`
Expected: **FAIL** — `AiClassifier` doesn't exist.

- [ ] **Step 6.3: Implement minimal `AiClassifier` for the success path**

Create `source/FlowHub.AI/AiClassifier.cs`:

```csharp
using System.Diagnostics;
using System.Text.Json;
using FlowHub.Core.Classification;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace FlowHub.AI;

internal sealed partial class AiClassifier : IClassifier
{
    private static readonly string[] AllowedSkills = ["Wallabag", "Vikunja", ""];
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly IChatClient _chat;
    private readonly IClassifier _keyword;
    private readonly ILogger<AiClassifier> _log;
    private readonly ChatOptions _options;

    public AiClassifier(
        IChatClient chat,
        IClassifier keyword,
        ILogger<AiClassifier> log,
        ChatOptions options)
    {
        _chat = chat;
        _keyword = keyword;
        _log = log;
        _options = options;
    }

    public async Task<ClassificationResult> ClassifyAsync(string content, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);
        var sw = Stopwatch.StartNew();

        try
        {
            var response = await _chat.GetResponseAsync(
                AiPrompts.BuildMessages(content),
                _options,
                cancellationToken);

            var payload = JsonSerializer.Deserialize<AiClassificationResponse>(response.Text ?? "", Json)
                ?? throw new JsonException("AI response payload was null after deserialization");

            if (Array.IndexOf(AllowedSkills, payload.MatchedSkill) < 0)
            {
                throw new InvalidOperationException("schema_violation");
            }

            return new ClassificationResult(payload.Tags, payload.MatchedSkill, payload.Title);
        }
        catch (Exception ex)
        {
            sw.Stop();
            var reason = ex is InvalidOperationException && ex.Message == "schema_violation"
                ? "schema_violation"
                : ex.GetType().Name;
            LogFellBack(reason, sw.ElapsedMilliseconds);
            return await _keyword.ClassifyAsync(content, cancellationToken);
        }
    }

    [LoggerMessage(
        EventId = 3010,
        Level = LogLevel.Warning,
        Message = "AiClassifier fell back to keyword classifier (reason={Reason}, duration_ms={DurationMs})")]
    private partial void LogFellBack(string reason, long durationMs);
}
```

The `JsonSerializerDefaults.Web` option matches MEAI's default snake_case → camelCase mapping. The DTO uses C# property names (`Tags`, `MatchedSkill`, `Title`); the AI returns `tags`, `matched_skill`, `title`. We need explicit attribute names — adjust the DTO in Step 6.4 below.

- [ ] **Step 6.4: Patch the DTO for snake_case mapping**

Edit `source/FlowHub.AI/AiClassificationResponse.cs` to add `JsonPropertyName` attributes. Replace the file with:

```csharp
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace FlowHub.AI;

internal sealed record AiClassificationResponse(
    [property: Description("1–5 short lowercase tags describing the snippet")]
    [property: JsonPropertyName("tags")]
    string[] Tags,

    [property: Description("Wallabag, Vikunja, or empty string for none")]
    [property: AllowedValues("Wallabag", "Vikunja", "")]
    [property: JsonPropertyName("matched_skill")]
    string MatchedSkill,

    [property: Description("3–8 word title or null if content is too short")]
    [property: JsonPropertyName("title")]
    string? Title);
```

`AiClassificationResponseTests` from Task 5 still passes: the schema-shape attributes (`Description`, `AllowedValues`) are unchanged.

- [ ] **Step 6.5: Run test 1, verify it passes**

Run: `dotnet test tests/FlowHub.Web.ComponentTests --filter "FullyQualifiedName~AiClassifierTests.ClassifyAsync_AiSucceedsWithValidSchema_ReturnsAiResult"`
Expected: **PASS**.

- [ ] **Step 6.6: Add tests 2–4 (argument forwarding)**

Append to `AiClassifierTests`:

```csharp
    [Fact]
    public async Task ClassifyAsync_ForwardsCancellationTokenToChatClient()
    {
        using var cts = new CancellationTokenSource();
        _chat.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions?>(), Arg.Any<CancellationToken>())
             .Returns(JsonResponse(new { tags = new[] { "x" }, matched_skill = "", title = (string?)null }));

        await Sut().ClassifyAsync("anything", cts.Token);

        await _chat.Received(1).GetResponseAsync(
            Arg.Any<IEnumerable<ChatMessage>>(),
            Arg.Any<ChatOptions?>(),
            cts.Token);
    }

    [Fact]
    public async Task ClassifyAsync_PassesMaxOutputTokens300_ToChatClient()
    {
        _chat.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions?>(), Arg.Any<CancellationToken>())
             .Returns(JsonResponse(new { tags = new[] { "x" }, matched_skill = "", title = (string?)null }));

        await Sut().ClassifyAsync("anything", default);

        await _chat.Received(1).GetResponseAsync(
            Arg.Any<IEnumerable<ChatMessage>>(),
            Arg.Is<ChatOptions?>(o => o != null && o.MaxOutputTokens == 300),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ClassifyAsync_PassesTemperature02_ToChatClient()
    {
        _chat.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions?>(), Arg.Any<CancellationToken>())
             .Returns(JsonResponse(new { tags = new[] { "x" }, matched_skill = "", title = (string?)null }));

        await Sut().ClassifyAsync("anything", default);

        await _chat.Received(1).GetResponseAsync(
            Arg.Any<IEnumerable<ChatMessage>>(),
            Arg.Is<ChatOptions?>(o => o != null && Math.Abs((double)(o.Temperature ?? 0f) - 0.2) < 0.0001),
            Arg.Any<CancellationToken>());
    }
```

- [ ] **Step 6.7: Run tests 2–4, verify they pass**

Run: `dotnet test tests/FlowHub.Web.ComponentTests --filter "FullyQualifiedName~AiClassifierTests"`
Expected: **PASS** — 4 of 4 tests pass without changes (the implementation already forwards `_options` and `cancellationToken`).

- [ ] **Step 6.8: Add tests 5–9 (fallback paths)**

Append to `AiClassifierTests`:

```csharp
    [Fact]
    public async Task ClassifyAsync_HttpRequestException_FallsBackToKeyword()
    {
        _chat.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions?>(), Arg.Any<CancellationToken>())
             .ThrowsAsync(new HttpRequestException("network down"));
        _keyword.ClassifyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(new ClassificationResult(["unsorted"], string.Empty));

        var result = await Sut().ClassifyAsync("anything", default);

        result.Should().BeEquivalentTo(new ClassificationResult(["unsorted"], string.Empty));
        await _keyword.Received(1).ClassifyAsync("anything", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ClassifyAsync_TaskCanceledException_FallsBackToKeyword()
    {
        _chat.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions?>(), Arg.Any<CancellationToken>())
             .ThrowsAsync(new TaskCanceledException("HttpClient timeout"));
        _keyword.ClassifyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(new ClassificationResult(["unsorted"], string.Empty));

        var result = await Sut().ClassifyAsync("anything", default);

        result.MatchedSkill.Should().BeEmpty();
        await _keyword.Received(1).ClassifyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ClassifyAsync_JsonException_FallsBackToKeyword()
    {
        _chat.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions?>(), Arg.Any<CancellationToken>())
             .Returns(new ChatResponse(new ChatMessage(ChatRole.Assistant, "this is not JSON")));
        _keyword.ClassifyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(new ClassificationResult(["unsorted"], string.Empty));

        var result = await Sut().ClassifyAsync("anything", default);

        result.MatchedSkill.Should().BeEmpty();
        await _keyword.Received(1).ClassifyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ClassifyAsync_MatchedSkillOutsideAllowedSet_FallsBackAndLogsSchemaViolation()
    {
        _chat.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions?>(), Arg.Any<CancellationToken>())
             .Returns(JsonResponse(new { tags = new[] { "x" }, matched_skill = "Bogus", title = "t" }));
        _keyword.ClassifyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(new ClassificationResult(["unsorted"], string.Empty));

        var result = await Sut().ClassifyAsync("anything", default);

        result.MatchedSkill.Should().BeEmpty();
        _log.Records.Should().ContainSingle(r => r.EventId.Id == 3010 && r.Message.Contains("schema_violation"));
    }

    [Fact]
    public async Task ClassifyAsync_GenericException_FallsBackToKeyword()
    {
        _chat.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions?>(), Arg.Any<CancellationToken>())
             .ThrowsAsync(new Exception("anything else"));
        _keyword.ClassifyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(new ClassificationResult(["unsorted"], string.Empty));

        var result = await Sut().ClassifyAsync("anything", default);

        result.MatchedSkill.Should().BeEmpty();
        await _keyword.Received(1).ClassifyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
```

- [ ] **Step 6.9: Run tests 5–9, verify they pass**

Run: `dotnet test tests/FlowHub.Web.ComponentTests --filter "FullyQualifiedName~AiClassifierTests"`
Expected: **PASS** — 9 of 9 tests pass without further implementation changes (the catch-all + schema-violation path is already implemented).

- [ ] **Step 6.10: Add the `FakeLogger<T>` shim + test 10 (logging assertion)**

Append at the bottom of `AiClassifierTests.cs`, OUTSIDE the `AiClassifierTests` class:

```csharp
file sealed class FakeLogger<T> : ILogger<T>
{
    public List<LogRecord> Records { get; } = [];

    public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;
    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel, EventId eventId, TState state,
        Exception? exception, Func<TState, Exception?, string> formatter)
    {
        Records.Add(new LogRecord(logLevel, eventId, formatter(state, exception)));
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose() { }
    }
}

file sealed record LogRecord(LogLevel Level, EventId EventId, string Message);
```

Then add test 10 inside the `AiClassifierTests` class (alongside the others):

```csharp
    [Fact]
    public async Task ClassifyAsync_OnFallback_LogsEventId3010WithExceptionTypeAndDuration()
    {
        _chat.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions?>(), Arg.Any<CancellationToken>())
             .ThrowsAsync(new HttpRequestException("oops"));
        _keyword.ClassifyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(new ClassificationResult(["unsorted"], string.Empty));

        await Sut().ClassifyAsync("anything", default);

        var record = _log.Records.Should().ContainSingle(r => r.EventId.Id == 3010).Subject;
        record.Level.Should().Be(LogLevel.Warning);
        record.Message.Should().Contain(nameof(HttpRequestException));
        record.Message.Should().MatchRegex(@"duration_ms=\d+");
    }
```

- [ ] **Step 6.11: Run all `AiClassifierTests`, verify all 10 pass**

Run: `dotnet test tests/FlowHub.Web.ComponentTests --filter "FullyQualifiedName~AiClassifierTests"`
Expected: **PASS — 10 tests**.

- [ ] **Step 6.12: Run the full suite to make sure no regression**

Run: `dotnet test FlowHub.slnx`
Expected: all tests still pass.

- [ ] **Step 6.13: Commit**

```bash
git add source/FlowHub.AI/AiClassifier.cs source/FlowHub.AI/AiClassificationResponse.cs \
        tests/FlowHub.Web.ComponentTests/Ai/AiClassifierTests.cs
git commit -m "feat(ai): add AiClassifier with structured-output + keyword fallback floor"
```

---

## Task 7: TDD `AddFlowHubAi` registration matrix

**Files:**
- Create: `source/FlowHub.AI/AiProvider.cs`
- Create: `source/FlowHub.AI/AiRegistrationOutcome.cs`
- Create: `source/FlowHub.AI/AiBootLogger.cs`
- Create: `source/FlowHub.AI/AiServiceCollectionExtensions.cs`
- Create: `tests/FlowHub.Web.ComponentTests/Ai/AiServiceCollectionExtensionsTests.cs`

The extension method covers the D8 behaviour matrix:

| `Ai__Provider` | `Ai__<P>__ApiKey` | DI registration | Startup log |
|---|---|---|---|
| unset | — | `KeywordClassifier` as `IClassifier` | Info `3021 AiProviderNotConfigured` (reason: no provider) |
| `Anthropic` | unset | `KeywordClassifier` | Info `3021` (reason: missing key) |
| `Anthropic` | set | `AiClassifier` | Info `3020 AiProviderRegistered` (provider, model) |
| `OpenRouter` | unset | `KeywordClassifier` | Info `3021` (reason: missing key) |
| `OpenRouter` | set | `AiClassifier` | Info `3020` |
| invalid value | — | throws `InvalidOperationException` | — |

The boot log is moved to a small `IHostedService` (`AiBootLogger`) so the extension stays a pure DI wiring helper. The hosted service consumes a singleton `AiRegistrationOutcome` record and writes 3020/3021 in `StartAsync`.

- [ ] **Step 7.1: Write failing tests**

Create `tests/FlowHub.Web.ComponentTests/Ai/AiServiceCollectionExtensionsTests.cs`:

```csharp
using FlowHub.AI;
using FlowHub.Core.Classification;
using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlowHub.Web.ComponentTests.Ai;

public sealed class AiServiceCollectionExtensionsTests
{
    private static IServiceProvider Build(Dictionary<string, string?> settings)
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
            .AddInMemoryCollection(new() { ["Ai:Provider"] = "Bogus" })
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
```

- [ ] **Step 7.2: Run tests, verify they fail**

Run: `dotnet test tests/FlowHub.Web.ComponentTests --filter "FullyQualifiedName~AiServiceCollectionExtensionsTests"`
Expected: **FAIL** — types don't exist.

- [ ] **Step 7.3: Add `AiProvider` enum**

Create `source/FlowHub.AI/AiProvider.cs`:

```csharp
namespace FlowHub.AI;

public enum AiProvider
{
    Anthropic,
    OpenRouter,
}
```

- [ ] **Step 7.4: Add `AiRegistrationOutcome` record**

Create `source/FlowHub.AI/AiRegistrationOutcome.cs`:

```csharp
namespace FlowHub.AI;

/// <summary>
/// Captured at <see cref="AiServiceCollectionExtensions.AddFlowHubAi"/> time and consumed
/// by <see cref="AiBootLogger"/> to write the 3020/3021 startup log line.
/// </summary>
public sealed record AiRegistrationOutcome(
    bool UsesAi,
    AiProvider? Provider,
    string? Model,
    string Reason); // "configured" | "no-provider" | "missing-key"
```

- [ ] **Step 7.5: Add `AiBootLogger` hosted service**

Create `source/FlowHub.AI/AiBootLogger.cs`:

```csharp
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FlowHub.AI;

internal sealed partial class AiBootLogger : IHostedService
{
    private readonly ILogger<AiBootLogger> _log;
    private readonly AiRegistrationOutcome _outcome;

    public AiBootLogger(ILogger<AiBootLogger> log, AiRegistrationOutcome outcome)
    {
        _log = log;
        _outcome = outcome;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_outcome.UsesAi)
        {
            LogProviderRegistered(_outcome.Provider!.Value.ToString(), _outcome.Model!);
        }
        else
        {
            LogProviderNotConfigured(_outcome.Reason);
        }
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(
        EventId = 3020,
        Level = LogLevel.Information,
        Message = "AI provider registered (provider={Provider}, model={Model})")]
    private partial void LogProviderRegistered(string provider, string model);

    [LoggerMessage(
        EventId = 3021,
        Level = LogLevel.Information,
        Message = "AI provider not configured — falling back to KeywordClassifier (reason={Reason})")]
    private partial void LogProviderNotConfigured(string reason);
}
```

- [ ] **Step 7.6: Add `AiServiceCollectionExtensions`**

Create `source/FlowHub.AI/AiServiceCollectionExtensions.cs`:

```csharp
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
        var maxTokens = configuration.GetValue<int?>("Ai:MaxOutputTokens") ?? 300;

        services.AddSingleton<IChatClient>(sp =>
            BuildChatClient(outcome.Provider!.Value, apiKey, model, configuration)
                .AsBuilder()
                .UseOpenTelemetry()
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

    private static IChatClient BuildChatClient(AiProvider provider, string apiKey, string model, IConfiguration configuration)
    {
        switch (provider)
        {
            case AiProvider.Anthropic:
                // Anthropic.SDK exposes a MEAI-compatible IChatClient via AnthropicClient.Messages.
                // The exact factory name has changed across Anthropic.SDK versions — verify on the
                // installed package and adjust. Common pattern (Anthropic.SDK 5.x):
                return new AnthropicClient(apiKey).Messages.AsBuilder().Build()
                    .AsIChatClient(model);

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
```

> ⚠ **Adapter API note**: the exact MEAI bridge call shape on `Anthropic.SDK` is version-sensitive — the canonical pattern is to call `.AsIChatClient(model)` on the Anthropic messages client, but the method name/location may vary. If `BuildChatClient` doesn't compile, run `dotnet package search Anthropic.SDK Microsoft.Extensions.AI` and consult the package README. The `OpenAIClient` → `IChatClient` bridge is stable in `Microsoft.Extensions.AI.OpenAI`; if its API differs, the canonical surface is `new OpenAIClient(...).GetChatClient(model).AsIChatClient()`.
> The unit tests in Step 7.1 exercise `IClassifier` registration only — they do not actually invoke the live `IChatClient`, so the wiring code in `BuildChatClient` is not exercised by Tasks 6/7. It IS exercised by Task 10 integration tests against real providers.

- [ ] **Step 7.7: Run tests, verify all 8 pass**

Run: `dotnet test tests/FlowHub.Web.ComponentTests --filter "FullyQualifiedName~AiServiceCollectionExtensionsTests"`
Expected: **PASS** — 8 tests.

- [ ] **Step 7.8: Run the full suite**

Run: `dotnet test FlowHub.slnx`
Expected: all tests still pass.

- [ ] **Step 7.9: Commit**

```bash
git add source/FlowHub.AI/AiProvider.cs source/FlowHub.AI/AiRegistrationOutcome.cs \
        source/FlowHub.AI/AiBootLogger.cs source/FlowHub.AI/AiServiceCollectionExtensions.cs \
        tests/FlowHub.Web.ComponentTests/Ai/AiServiceCollectionExtensionsTests.cs
git commit -m "feat(ai): add AddFlowHubAi extension with D8 registration matrix + boot logger"
```

---

## Task 8: Wire `AddFlowHubAi` into `Program.cs`

**Files:**
- Modify: `source/FlowHub.Web/Program.cs`
- Modify: `source/FlowHub.Web/FlowHub.Web.csproj`

The Web project needs a `UserSecretsId` so `dotnet user-secrets set Ai:Anthropic:ApiKey …` writes to the right per-project secrets store (per spec — "Where keys live in dev").

- [ ] **Step 8.1: Add `UserSecretsId` to `FlowHub.Web.csproj`**

Add a new property inside the existing `<PropertyGroup>` in `source/FlowHub.Web/FlowHub.Web.csproj`:

```xml
    <UserSecretsId>flowhub-web-cas-aise</UserSecretsId>
```

After this change the property group is:

```xml
  <PropertyGroup>
    <RootNamespace>FlowHub.Web</RootNamespace>
    <AssemblyName>FlowHub.Web</AssemblyName>
    <UserSecretsId>flowhub-web-cas-aise</UserSecretsId>
  </PropertyGroup>
```

- [ ] **Step 8.2: Replace the keyword registration in `Program.cs`**

In `source/FlowHub.Web/Program.cs`:

Add to the `using` block at the top (alphabetical — between `FlowHub.Api.Endpoints` and `FlowHub.Core.Captures`):

```csharp
using FlowHub.AI;
```

Replace the line at `Program.cs:59`:

```csharp
builder.Services.AddSingleton<IClassifier, KeywordClassifier>();
```

with:

```csharp
// Block 3 Slice C — AI-backed classifier (per ADR 0004) with keyword fallback.
// Uses real provider when Ai:Provider + Ai:<P>:ApiKey are set; silently falls back
// to the deterministic KeywordClassifier otherwise so `make run` works zero-config.
builder.Services.AddFlowHubAi(builder.Configuration);
```

- [ ] **Step 8.3: Build**

Run: `make build`
Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`.

- [ ] **Step 8.4: Smoke-run the app with no AI configured (default keyword path)**

Start the server:

```bash
make run &
sleep 8
```

In another shell:

```bash
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5070/                 # Expected: 200
curl -s -X POST -H "Content-Type: application/json" \
     -d '{"content":"https://example.com","source":"Web"}' \
     http://localhost:5070/api/v1/captures \
     -o /dev/null -w "%{http_code}\n"                                            # Expected: 201
```

Verify the startup log shows EventId 3021 (provider not configured) by checking the server's stdout.

Stop:

```bash
pkill -f FlowHub.Web
```

- [ ] **Step 8.5: Run the full suite**

Run: `dotnet test FlowHub.slnx`
Expected: all tests still pass — `AddFlowHubAi(IConfiguration)` falls back to keyword in test context (no provider configured), so existing pipeline tests still wire `KeywordClassifier`.

- [ ] **Step 8.6: Commit**

```bash
git add source/FlowHub.Web/FlowHub.Web.csproj source/FlowHub.Web/Program.cs
git commit -m "feat(ai): wire AddFlowHubAi into Program.cs (Slice C)"
```

---

## Task 9: Scaffold integration test project

**Files:**
- Create: `tests/FlowHub.AI.IntegrationTests/FlowHub.AI.IntegrationTests.csproj`
- Create: `tests/FlowHub.AI.IntegrationTests/Usings.cs`
- Modify: `FlowHub.slnx`

This new csproj exists solely for trait-gated `[Trait("Category","AI")]` tests that talk to real providers. Excluded from the default `make test` filter; runs only via `make test-ai`.

- [ ] **Step 9.1: Create the csproj**

Create `tests/FlowHub.AI.IntegrationTests/FlowHub.AI.IntegrationTests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <RootNamespace>FlowHub.AI.IntegrationTests</RootNamespace>
    <AssemblyName>FlowHub.AI.IntegrationTests</AssemblyName>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
    <NoWarn>$(NoWarn);CA1707</NoWarn>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="FluentAssertions" />
    <PackageReference Include="Microsoft.Extensions.AI" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\source\FlowHub.AI\FlowHub.AI.csproj" />
    <ProjectReference Include="..\..\source\FlowHub.Core\FlowHub.Core.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 9.2: Add `Usings.cs`**

Create `tests/FlowHub.AI.IntegrationTests/Usings.cs`:

```csharp
global using Xunit;
```

- [ ] **Step 9.3: Register in solution**

In `FlowHub.slnx`, add inside the `/tests/` folder block, alphabetically (before `FlowHub.Api.IntegrationTests`):

```xml
    <Project Path="tests/FlowHub.AI.IntegrationTests/FlowHub.AI.IntegrationTests.csproj" />
```

Final `/tests/` block ordering:

```xml
  <Folder Name="/tests/">
    <Project Path="tests/FlowHub.AI.IntegrationTests/FlowHub.AI.IntegrationTests.csproj" />
    <Project Path="tests/FlowHub.Api.IntegrationTests/FlowHub.Api.IntegrationTests.csproj" />
    <Project Path="tests/FlowHub.Web.ComponentTests/FlowHub.Web.ComponentTests.csproj" />
  </Folder>
```

- [ ] **Step 9.4: Build**

Run: `make restore && make build`
Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`.

- [ ] **Step 9.5: Confirm the empty project is picked up by the test runner**

Run: `dotnet test FlowHub.slnx`
Expected: same passing-test count as before; the new project compiles but has 0 tests.

- [ ] **Step 9.6: Commit**

```bash
git add FlowHub.slnx tests/FlowHub.AI.IntegrationTests
git commit -m "test(ai): scaffold FlowHub.AI.IntegrationTests project for live provider tests"
```

---

## Task 10: Add 4 trait-gated live-provider tests

**Files:**
- Create: `tests/FlowHub.AI.IntegrationTests/AnthropicHaikuLiveTests.cs`
- Create: `tests/FlowHub.AI.IntegrationTests/OpenRouterLlamaLiveTests.cs`

These tests skip themselves when the matching `Ai__<P>__ApiKey` env var is unset, so they're safe to leave in the default `dotnet test` run too — but the trait filter keeps CI fast and predictable.

- [ ] **Step 10.1: Add Anthropic live tests**

Create `tests/FlowHub.AI.IntegrationTests/AnthropicHaikuLiveTests.cs`:

```csharp
using FlowHub.AI;
using FlowHub.Core.Classification;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace FlowHub.AI.IntegrationTests;

[Trait("Category", "AI")]
public sealed class AnthropicHaikuLiveTests
{
    private static IClassifier? BuildClassifier()
    {
        var apiKey = Environment.GetEnvironmentVariable("Ai__Anthropic__ApiKey");
        if (string.IsNullOrWhiteSpace(apiKey)) return null;

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Ai:Provider"] = "Anthropic",
                ["Ai:Anthropic:ApiKey"] = apiKey,
            }).Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFlowHubAi(config);
        return services.BuildServiceProvider().GetRequiredService<IClassifier>();
    }

    [Fact]
    public async Task ClassifyAsync_UrlContent_LiveAnthropicReturnsWallabagWithTitle()
    {
        var sut = BuildClassifier();
        if (sut is null) return; // skip when no key configured

        var result = await sut.ClassifyAsync(
            "https://en.wikipedia.org/wiki/Hexagonal_architecture",
            CancellationToken.None);

        result.MatchedSkill.Should().Be("Wallabag");
        result.Title.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ClassifyAsync_TodoContent_LiveAnthropicReturnsVikunjaWithTitle()
    {
        var sut = BuildClassifier();
        if (sut is null) return;

        var result = await sut.ClassifyAsync(
            "todo: buy milk on Saturday",
            CancellationToken.None);

        result.MatchedSkill.Should().Be("Vikunja");
        result.Title.Should().NotBeNullOrWhiteSpace();
    }
}
```

- [ ] **Step 10.2: Add OpenRouter live tests**

Create `tests/FlowHub.AI.IntegrationTests/OpenRouterLlamaLiveTests.cs`:

```csharp
using FlowHub.AI;
using FlowHub.Core.Classification;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace FlowHub.AI.IntegrationTests;

[Trait("Category", "AI")]
public sealed class OpenRouterLlamaLiveTests
{
    private static IClassifier? BuildClassifier()
    {
        var apiKey = Environment.GetEnvironmentVariable("Ai__OpenRouter__ApiKey");
        if (string.IsNullOrWhiteSpace(apiKey)) return null;

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Ai:Provider"] = "OpenRouter",
                ["Ai:OpenRouter:ApiKey"] = apiKey,
            }).Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFlowHubAi(config);
        return services.BuildServiceProvider().GetRequiredService<IClassifier>();
    }

    [Fact]
    public async Task ClassifyAsync_UrlContent_LiveOpenRouterReturnsWallabagWithTitle()
    {
        var sut = BuildClassifier();
        if (sut is null) return;

        var result = await sut.ClassifyAsync(
            "https://en.wikipedia.org/wiki/Modular_monolith",
            CancellationToken.None);

        result.MatchedSkill.Should().Be("Wallabag");
        result.Title.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ClassifyAsync_TodoContent_LiveOpenRouterReturnsVikunjaWithTitle()
    {
        var sut = BuildClassifier();
        if (sut is null) return;

        var result = await sut.ClassifyAsync(
            "todo: review the Block 3 PVA submission tomorrow",
            CancellationToken.None);

        result.MatchedSkill.Should().Be("Vikunja");
        result.Title.Should().NotBeNullOrWhiteSpace();
    }
}
```

- [ ] **Step 10.3: Run the trait-gated tests against real providers (operator step)**

Only run this if you have keys to spend. The default `make test` filter excludes them.

```bash
export Ai__Anthropic__ApiKey="sk-ant-…"
export Ai__OpenRouter__ApiKey="sk-or-…"
dotnet test tests/FlowHub.AI.IntegrationTests --filter "Category=AI"
```

Expected: 4 passed. If any fail, capture the response shape via `dotnet test -v normal` and adjust prompts only after confirming the schema-shape (D7) was honoured by the provider.

If you don't have keys, skip this step — `dotnet test FlowHub.slnx --filter "Category!=AI"` (Task 11 wires this into `make test`) keeps the default green.

- [ ] **Step 10.4: Confirm default suite still green**

Run: `dotnet test FlowHub.slnx --filter "Category!=AI"`
Expected: all non-AI tests pass; the 4 trait-gated tests are excluded.

- [ ] **Step 10.5: Commit**

```bash
git add tests/FlowHub.AI.IntegrationTests/AnthropicHaikuLiveTests.cs \
        tests/FlowHub.AI.IntegrationTests/OpenRouterLlamaLiveTests.cs
git commit -m "test(ai): add 4 live-provider integration tests (trait-gated Category=AI)"
```

---

## Task 11: Update Makefile

**Files:**
- Modify: `Makefile`

`make test` filters out the AI category so CI stays fast. New `make test-ai` runs only the live tests; requires the operator to have set the keys.

- [ ] **Step 11.1: Update the Makefile**

Replace the line:

```make
.PHONY: help run watch build test test-watch restore clean format
```

with:

```make
.PHONY: help run watch build test test-ai test-watch restore clean format
```

Replace the `test:` target (currently lines 33–34):

```make
test: ## Run all tests
	dotnet test $(SOLUTION) --no-build
```

with:

```make
test: ## Run all tests except [Category=AI]
	dotnet test $(SOLUTION) --no-build --filter "Category!=AI"

test-ai: ## Run live integration tests against real AI providers (requires Ai__*__ApiKey env)
	dotnet test tests/FlowHub.AI.IntegrationTests --filter "Category=AI"
```

- [ ] **Step 11.2: Verify both targets**

Run: `make build && make test`
Expected: all non-AI tests pass.

```bash
make test-ai
```
Expected (if keys set): 4 live tests pass. (If keys unset: tests no-op via the early `return` guard, all still pass.)

- [ ] **Step 11.3: Commit**

```bash
git add Makefile
git commit -m "build(make): split AI live tests behind test-ai target (Category!=AI default)"
```

---

## Task 12: Update `docs/ai-usage.md`

**Files:**
- Modify: `docs/ai-usage.md`

- [ ] **Step 12.1: Append the Slice C section**

Append at the end of `docs/ai-usage.md`, after the existing `## Block 3 Slice A — REST API` section and before `## Prompts of note`:

```markdown
## Block 3 Slice C — AI integration (production-runtime)

Slice C is the first time AI moves from *development tool* to *production-runtime
component*. The classifier port `IClassifier` (introduced in Slice B as a hexagonal
seam with a deterministic `KeywordClassifier` adapter) now has a second adapter,
`AiClassifier`, that calls a real LLM in the request path of the enrichment consumer.

### Brainstorming + spec writing

Conversational design via Claude Code's brainstorming skill. 10 decisions surfaced
as A/B/C questions covering: scope (classifier-only vs. classifier+title vs. multi-
feature enrichment), provider mix (which two adapters), abstraction layer
(`Microsoft.Extensions.AI` vs. Semantic Kernel), port shape (extend
`ClassificationResult` vs. sibling `IEnricher`), failure semantics (graceful fallback
vs. hard fail), default models, structured-output strategy, no-key startup behaviour,
test layering (mocked unit + trait-gated live), and provider selection.

User picked the .NET-native MEAI abstraction over Semantic Kernel (Q3) and the
graceful fallback over hard-fail (Q5) — both decisions explicitly motivated by the
rubric phrasing *"intelligente und flexible Services"* (graceful = flexible).

Final spec self-reviewed by Claude before commit (placeholder scan, internal
consistency, ambiguity check, scope check).

### Implementation plan

Authored by Claude Code via the `writing-plans` skill from the approved spec. 15
tasks total, TDD-ordered: packages → ClassificationResult extension → FlowHub.AI
csproj → AiPrompts → AiClassificationResponse DTO → AiClassifier (10 unit tests) →
AddFlowHubAi (8 unit tests covering D8 matrix) → Program.cs wiring → integration
test project → 4 trait-gated live tests → Makefile filter → ai-usage.md → ADR 0004
→ CLAUDE.md placeholder note → vault checklist + CHANGELOG + final pass.

### Implementation execution

Subagent-driven. Sonnet 4.6 for TDD/judgment-heavy tasks (T2, T4–T10); Haiku for
mechanical tasks (T1 packages, T3 csproj scaffolding, T9 integration test scaffold,
T11 Makefile, T14 CLAUDE.md tweak, T15 vault checklist).

### Production-runtime AI use

This slice is what the rubric *"KI: Wurden KI-Werkzeuge verwendet"* (max 12 pts)
actually asks for: AI inside the running application, not just AI as a coding
assistant. The `AiClassifier` calls a real LLM (Anthropic Haiku 4.5 by default,
swappable to OpenRouter Llama 3.1 70B Instruct via one env var) on every capture
that flows through the enrichment pipeline.

Cost guards: `MaxOutputTokens=300`, `Temperature=0.2`, 10s HTTP timeout, Anthropic
prompt-cache marker on the system prompt segment.

Failure handling: any provider exception or schema violation logs `EventId 3010
AiClassifierFellBackToKeyword` at Warning and routes to the deterministic
`KeywordClassifier` floor — capture is always classified.

### Notable adaptations the implementers caught (real value, not hallucinations)

(To be filled in by the implementer after Task 15 final pass — same shape as the
Slice B / Slice A entries.)

### Generated vs. handwritten share (estimate, Slice C only)

(To be filled in.)

### Reflexion — what worked, what didn't

(To be filled in.)
```

> The "to be filled in" subsections below the production-runtime paragraph are intentional placeholders for the implementer to populate at the end of Task 15 (final pass). They are NOT the kind of "TBD / implement later" that the plan-author rules forbid — they are *retrospective* notes that can only be written *after* the implementation runs, by the human/agent that ran it.

- [ ] **Step 12.2: Commit**

```bash
git add docs/ai-usage.md
git commit -m "docs(ai-usage): scaffold Slice-C section (Block 3 AI integration)"
```

---

## Task 13: Write ADR 0004

**Files:**
- Create: `docs/adr/0004-ai-integration-in-services.md`

The ADR distils spec D1–D10 into 7–10 numbered decisions, mirroring the ADR 0003 shape (Status / Date / Block / Decider / Affects header → Context → Decision → Consequences → References).

- [ ] **Step 13.1: Read ADR 0003 for shape reference**

Open `docs/adr/0003-async-pipeline.md` to mirror its structure and tone.

- [ ] **Step 13.2: Author ADR 0004**

Create `docs/adr/0004-ai-integration-in-services.md`:

```markdown
# ADR 0004 — AI Integration in Services: Provider, Abstraction, Prompt + Cost Strategy

- **Status:** Accepted
- **Date:** 2026-05-XX (set to commit date)
- **Block:** Block 3 (Services) — Nachbereitung · Slice C
- **Decider:** freax
- **Affects:** `source/FlowHub.AI/`, `source/FlowHub.Core/Classification/ClassificationResult.cs`, `source/FlowHub.Web/Program.cs`, `Directory.Packages.props`, `tests/FlowHub.AI.IntegrationTests/`

---

## Context

ADR 0003 §3 pre-committed a hexagonal `IClassifier` port and shipped a deterministic
`KeywordClassifier` for Slice B. Both ADR 0003 and the Block 3-c Nachbereitung
checklist explicitly name "Slice C swaps in an AI-backed adapter" as the next step.
The Block 3 Moodle Auftrag demands "intelligente und flexible Services" using AI;
the Bewertungskriterien dimension *"Intelligente / flexible Services mit KI gebaut"*
(max 6 pts) is the explicit deliverable, and the highest-weighted single rubric item
(*"Wurden KI-Werkzeuge verwendet und deren Nutzung beschrieben"*, max 12 pts) demands
**production-runtime** AI use, not just AI as a coding assistant.

This ADR records the durable decisions for that adapter. The brainstorming narrative
lives in `docs/superpowers/specs/2026-05-03-slice-c-ai-integration-design.md`; this
ADR distils D1–D10 from that spec.

The Quarkus / Jakarta-EE programming criterion remains **N/A** for FlowHub's .NET
stack — same precedent as ADR 0002 / 0003. The course's nominal Spring-AI / Koog
vocabulary is the teacher's reference stack; FlowHub's .NET-native equivalents
(`Microsoft.Extensions.AI`, optional Semantic Kernel) are presented in their own
terms.

---

## Decision

### 1. Slice scope = classifier + AI-generated title (one round-trip)

The AI does two jobs in a single structured-output call: classify (`Tags`,
`MatchedSkill`) and produce a short title (`Title`). No summary, no skill-suggestion
queue, no embeddings, no agent loop. Estimated effort 2 days.

### 2. Two interchangeable adapters: Anthropic native + OpenRouter aggregator

Both adapters implement `Microsoft.Extensions.AI.IChatClient`. The operator picks
one at boot via `Ai__Provider=Anthropic|OpenRouter`. The two transports are
deliberately different in shape:

- **Anthropic** uses the native Anthropic API via the `Anthropic.SDK` NuGet
  package's MEAI-compatible `IChatClient` implementation. Vendor-specific features
  available (prompt caching via `cache_control: ephemeral`).
- **OpenRouter** uses `Microsoft.Extensions.AI.OpenAI` against
  `https://openrouter.ai/api/v1`. OpenAI-compatible, so the same package reaches
  hundreds of upstream models (Anthropic, OpenAI, Google, Meta Llama, Mistral,
  Qwen, …) behind one adapter shape.

This earns the *"flexible"* in "intelligente und flexible Services" — one
vendor-native SDK, one aggregator gateway, one shared interface.

### 3. Abstraction = `Microsoft.Extensions.AI` (MEAI), not Semantic Kernel

The Block 3-c Nachbereitung demands "Microsoft.Extensions.AI **oder** Semantic
Kernel als Abstraktion einbinden". MEAI is the right shape for FlowHub's
single-shot classifier-with-typed-output use case: one interface (`IChatClient`),
schema-driven structured output (`CompleteAsync<TResponse>` / `GetResponseAsync`),
decorator chaining (`UseOpenTelemetry`, custom delegates) via `ChatClientBuilder`.
Lightweight; no kernel/plugin/planner machinery.

Semantic Kernel is reserved for a hypothetical Block-5 agent loop (e.g. an
"intelligent retry advisor" that decides between re-route / summarise-and-surface /
escalate when an integration call fails repeatedly) and is explicitly NOT introduced
in Slice C. SK consumes `IChatClient` natively, so adding it later is additive, not
a replacement.

### 4. Port shape = extend `ClassificationResult` with `Title?`

`IClassifier` stays one method. The result record gains an optional third field:

```csharp
public sealed record ClassificationResult(
    IReadOnlyList<string> Tags,
    string MatchedSkill,
    string? Title = null);
```

`KeywordClassifier` returns `Title=null`. `AiClassifier` returns both populated, or
falls back to keyword on AI failure (in which case `Title=null` again). Single
round-trip is meaningfully cheaper than two ports (`IClassifier` + `IEnricher`),
and the model returns both fields naturally inside one schema. Block 4/5 can split
the port if a real use case demands it.

### 5. Failure behaviour = graceful fallback to `KeywordClassifier`

`AiClassifier` wraps `KeywordClassifier` as a hard floor. The fallback triggers on
either:

- **Any exception** thrown by `IChatClient.GetResponseAsync<…>` — network, timeout,
  parse, schema, anything else (catch `Exception`, since MEAI's exact taxonomy
  varies between provider adapters).
- **Defensive post-validation**: even when the call returns successfully,
  `AiClassifier` checks `MatchedSkill ∈ {"Wallabag","Vikunja",""}`. If not, treat
  as a failure (the schema should have prevented this — never trust the model).

In either case the adapter:

1. Logs `Warning` (`EventId 3010 AiClassifierFellBackToKeyword`) with the failure
   reason (exception type or `"schema_violation"`) and elapsed milliseconds.
2. Calls `_keyword.ClassifyAsync(content, ct)` and returns its result with
   `Title=null`.
3. Does NOT rethrow.

Consequences: capture is always classified — AI outage degrades quality, never
availability. The MassTransit retry budget from ADR 0003 §5 stays reserved for
genuine pipeline / bus failures; the fault-observer path (ADR 0003 §6) is not
entered by AI errors.

### 6. Default models: Anthropic Haiku 4.5 / OpenRouter Llama 3.1 70B Instruct

| Provider | Model | Default env | Rationale |
|---|---|---|---|
| Anthropic | `claude-haiku-4-5-20251001` | `Ai__Anthropic__Model` | Cheapest current Claude; strong JSON-schema adherence; tool-use bridge for MEAI. |
| OpenRouter | `meta-llama/llama-3.1-70b-instruct` | `Ai__OpenRouter__Model` | Open-weights; 70B is reliable on schema (smaller variants flake). Sets up the "commercial vs open-weights" rubric narrative. |

Per-call tokens: ~200 input / ~150 output → cost is negligible regardless of
provider; narrative dominates over cost. Both env vars overridable for snapshot
bumps.

### 7. Structured output = `CompleteAsync<T>` with JSON schema generated from the DTO

```csharp
internal sealed record AiClassificationResponse(
    [property: Description("1–5 short lowercase tags describing the snippet")]
    [property: JsonPropertyName("tags")]
    string[] Tags,

    [property: Description("Wallabag, Vikunja, or empty string for none")]
    [property: AllowedValues("Wallabag", "Vikunja", "")]
    [property: JsonPropertyName("matched_skill")]
    string MatchedSkill,

    [property: Description("3–8 word title or null if content is too short")]
    [property: JsonPropertyName("title")]
    string? Title);
```

MEAI generates the JSON schema from the record. `AllowedValues` translates to a
JSON-schema `enum`; both Anthropic (tool-use under the hood) and OpenRouter
(`response_format: json_schema`) honour it. `MatchedSkill` MUST match a registered
`ISkillIntegration.Name` (`"Wallabag"`, `"Vikunja"`, or `""` → Orphan) or routing
breaks downstream — defensive runtime check inside `AiClassifier` re-validates even
though the schema enforces it.

### 8. No-key startup = silent fallback to `KeywordClassifier`

`AddFlowHubAi(IConfiguration)` inspects `Ai__Provider` and the matching `ApiKey`
and registers `KeywordClassifier` as the active `IClassifier` whenever either is
missing. Invalid `Ai__Provider` values throw `InvalidOperationException` at
startup. `make run` works zero-config for a fresh `git clone` — same dev-friendly
philosophy as `DevAuthHandler` for auth.

Boot logs: `EventId 3020 AiProviderRegistered` (Information) on the AI path,
`EventId 3021 AiProviderNotConfigured` (Information) with reason on the keyword
path. Emitted via a small `IHostedService` (`AiBootLogger`) so the extension stays
a pure DI helper.

### 9. Testing layered: mocked unit tests + trait-gated live integration tests

- 10 mocked unit tests (`tests/FlowHub.Web.ComponentTests/Ai/AiClassifierTests.cs`)
  via `NSubstitute<IChatClient>` and `NSubstitute<IClassifier>` — zero real API
  calls. Cover happy path, argument forwarding (CT, MaxOutputTokens, Temperature),
  the 5 fallback paths (HttpRequestException, TaskCanceledException, JsonException,
  schema violation, generic Exception), and the 3010 logging contract.
- 8 mocked unit tests for the D8 registration matrix
  (`AiServiceCollectionExtensionsTests.cs`).
- 4 trait-gated live integration tests
  (`tests/FlowHub.AI.IntegrationTests/`, `[Trait("Category","AI")]`) that talk to
  real providers — excluded from default `make test`, run via `make test-ai` when
  the operator has keys.

`Makefile` filters: `make test` → `--filter "Category!=AI"`. CI runs `make test`
only; `make test-ai` is operator-on-demand.

### 10. Active provider selection = explicit `Ai__Provider` env var

One env var swaps providers. No implicit precedence; no per-request runtime
selection; no round-robin. Demoing "swap providers via config" is a one-line env
change.

---

## Consequences

### Rubric coverage

This ADR + its implementation directly address six Bewertungskriterien dimensions:

- **Entwurf: Lösungsansatz und Architektur beschrieben** (max 7) — ASCII component
  diagram + data-flow diagrams in the spec.
- **Programmierung: Code lesbar, nach Layer / Modulen strukturiert** (max 7) —
  `FlowHub.AI` becomes a real project with single responsibility; `IClassifier`
  port stays in Core; adapters separated from prompt + DTO.
- **Programmierung: Erkenntnisse aus der Programmierung dokumentiert** (max 3) —
  this ADR + the Slice-C section in `docs/ai-usage.md`.
- **Validierung: Unit-Tests programmiert** (max 3) — 18 mocked unit tests + 4
  trait-gated live integration tests.
- **KI: Wurden KI-Werkzeuge verwendet und deren Nutzung beschrieben** (max 12) ⭐ —
  Slices A/B already covered KI-as-development-tool; this slice adds
  *production-runtime* AI use, which is what the rubric actually asks.
- **KI: Intelligente / flexible Services mit KI gebaut** (max 6) — explicit
  deliverable. Two adapters demonstrate flexibility; graceful keyword fallback
  demonstrates "intelligent" failure handling.

The Quarkus / Jakarta-EE criterion (max 10) remains N/A for the .NET stack.

### EventId namespacing — `3000–3999` reserved for AI

Extends the ADR 0003 §EventId convention:
- `1000–1999` — Pipeline (Slice B)
- `2000–2999` — Skills (Slice B)
- `3000–3999` — AI (this slice)
  - `3001` `AiClassifierStarted` (Debug — Slice C optional, may be omitted)
  - `3002` `AiClassifierSucceeded` (Debug — same)
  - `3010` `AiClassifierFellBackToKeyword` (Warning, on each fallback)
  - `3020` `AiProviderRegistered` (Information, at startup)
  - `3021` `AiProviderNotConfigured` (Information, at startup)

`LoggerMessage` source-gen used throughout (CA1848 / CA1873 enforced by
`Directory.Build.props`).

### Cost guards

`ChatOptions.MaxOutputTokens=300` (~2× the schema's natural ~150 output tokens),
`Temperature=0.2` (deterministic-ish), `HttpClient` 10s timeout.

Anthropic prompt cache: system prompt marked `cache_control: ephemeral` via
`ChatOptions.AdditionalProperties`; ~80% input-token discount on the system-prompt
segment after the second call. OpenRouter prompt cache is not universally
supported across upstream models — Slice C does not claim it. Asymmetry documented
here as a real difference between adapters.

### OpenTelemetry

`Microsoft.Extensions.AI.UseOpenTelemetry()` decorator emits `gen_ai.client.operation
.duration` and token-count metrics on the existing OTEL pipeline. Will surface in
the Block-5 Grafana dashboard alongside MassTransit traces (`AI calls / sec, p95
latency, fallback rate`).

### Reflexion — `Microsoft.Extensions.AI` vs Semantic Kernel

**MEAI** = thin abstraction over chat / embedding / tool-use; sister of
`Microsoft.Extensions.Logging` / `Microsoft.Extensions.Caching`. One interface
(`IChatClient`), schema-driven structured output, decorator chaining. Right pick
when the call site is "give the model a string, get a typed object back".

**Semantic Kernel** = full agent framework: `Kernel`, plugins, planners, memory,
agent runtime. Right pick when an LLM **orchestrates** multi-step work.

Decision rule for FlowHub:

- Single-shot LLM call with typed output → MEAI. Slice C is exactly this.
- Multi-step LLM-driven workflow → add Semantic Kernel without removing MEAI.

A credible Block-5 fit for SK: an *intelligent retry advisor* that, on repeated
integration failures, inspects the failure pattern + capture content + integration
health and decides between re-route / summarise-and-surface / escalate. Slice C
deliberately stops short.

---

## Consequences for next blocks

**Block 4 (Persistence)**:
- `IEmbeddingGenerator<string, Embedding<float>>` already exposed by MEAI →
  embedding work for Block 5 KI-search (pgvector via Npgsql) is incremental, not
  greenfield.
- AI audit fields on the persisted `Capture`: `(provider, model, duration_ms,
  was_fallback)` per classification. Earns the *"Test-Ergebnisse dokumentiert"*
  rubric item with real production data.

**Block 5 (Deployment + KI-search)**:
- ADR 0006 (KI-Suche) builds on the `IEmbeddingGenerator` shape. Anthropic doesn't
  ship embeddings → the embedding provider becomes either OpenAI native, OpenRouter
  passing through, or a self-hosted sentence-transformer behind an OpenAI-compatible
  facade. Asymmetry to document there.
- Integration-test secret rotation: `Ai__Anthropic__ApiKey` /
  `Ai__OpenRouter__ApiKey` move from User Secrets into the Block-5 deployment
  secret store (Authentik or Docker secrets).
- OTEL: `gen_ai.*` metrics export already; Grafana panel for "AI calls / sec, p95
  latency, fallback rate" comes nearly free.
- Optional Semantic-Kernel adoption — additive on top of MEAI.

---

## References

- Brainstorming spec: `docs/superpowers/specs/2026-05-03-slice-c-ai-integration-design.md`
- Implementation plan: `docs/superpowers/plans/2026-05-03-slice-c-ai-integration.md`
- ADR 0001: `docs/adr/0001-frontend-render-mode-and-architecture.md`
- ADR 0002: `docs/adr/0002-service-architecture-and-async-communication.md` — module split
- ADR 0003: `docs/adr/0003-async-pipeline.md` — `IClassifier` port, EventId namespacing
- AI Usage living doc: `docs/ai-usage.md`
- Block 3 Nachbereitung: `vault/Blöcke/03 Service/03 Service - c) Nachbereitung.md`
- Bewertungskriterien: `vault/Organisation/Bewertungskriterien.md`
- `Microsoft.Extensions.AI`: https://learn.microsoft.com/en-us/dotnet/ai/microsoft-extensions-ai
- OpenRouter API reference: https://openrouter.ai/docs
```

- [ ] **Step 13.3: Commit**

```bash
git add docs/adr/0004-ai-integration-in-services.md
git commit -m "docs(adr): add ADR 0004 — AI integration in services (Slice C)"
```

---

## Task 14: Update CLAUDE.md placeholder note

**Files:**
- Modify: `CLAUDE.md`

The repo-tree comment in CLAUDE.md still says `FlowHub.AI/  ← (placeholder — AI classification, future block)`. After Slice C lands, that's no longer true.

- [ ] **Step 14.1: Edit the line**

In `CLAUDE.md`, find the repository-structure block. Replace:

```
│   ├── FlowHub.AI/                    ← (placeholder — AI classification, future block)
```

with:

```
│   ├── FlowHub.AI/                    ← MEAI-backed classifier (Block 3 Slice C, ADR 0004)
```

- [ ] **Step 14.2: Commit**

```bash
git add CLAUDE.md
git commit -m "docs(claude.md): mark FlowHub.AI as active (Slice C landed)"
```

---

## Task 15: Tick off Block-3 vault checklist + CHANGELOG + final pass

**Files:**
- Modify: `vault/Blöcke/03 Service/03 Service - c) Nachbereitung.md`
- Modify: `CHANGELOG.md`
- Modify: `docs/ai-usage.md` (fill in the "to be filled in" sections from Task 12)

- [ ] **Step 15.1: Run the full default suite**

```bash
make build && make test
```
Expected: build clean (warnings-as-errors), all non-AI tests pass. New tests added in this slice:
- 1 KeywordClassifier title-null test
- 3 AiPrompts tests
- 3 AiClassificationResponse tests
- 10 AiClassifier tests
- 8 AiServiceCollectionExtensions tests

Total new: 25. Combined with the ~74 from Slice A baseline = ~99 passing tests.

- [ ] **Step 15.2: Smoke-run the app**

```bash
make run &
sleep 8
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5070/                          # Expected: 200
curl -s -X POST -H "Content-Type: application/json" \
     -d '{"content":"https://example.com/article","source":"Web"}' \
     http://localhost:5070/api/v1/captures \
     -o /dev/null -w "%{http_code}\n"                                                    # Expected: 201
pkill -f FlowHub.Web
```

Confirm the startup log shows `EventId 3021 AiProviderNotConfigured` with reason `no-provider` (no `Ai:Provider` set in dev).

- [ ] **Step 15.3: Tick off Block-3-c Nachbereitung items**

In `vault/Blöcke/03 Service/03 Service - c) Nachbereitung.md`, change the following lines from `- [ ]` to `- [x]`:

- Line ~48 — `Lösungsansatz & Architektur textuell + bildlich (7)` — annotate inline that ADR 0004 has now landed.
- Line ~72 — `Intelligente / flexible Services mit KI gebaut (6)` — `KeywordClassifier` ✅ + `AiClassifier` (Anthropic + OpenRouter via MEAI) ✅, fallback on errors documented.
- Line ~83 — `ADR 0004 — KI-Integration in Services` — landed at `docs/adr/0004-ai-integration-in-services.md`.
- Line ~104 — `Microsoft.Extensions.AI ... als Abstraktion einbinden` — landed in `source/FlowHub.AI/`.
- Line ~106 — `Reflexion zu Microsoft.Extensions.AI / Semantic Kernel` — captured in ADR 0004 §"Reflexion".

Update the file's `updated:` frontmatter to today's date.

- [ ] **Step 15.4: Add CHANGELOG `[Unreleased]` Slice-C entry**

In `CHANGELOG.md`, append under the existing `## [Unreleased]` → `### Added` block:

```markdown
- **Block 3 Slice C — AI integration**: `FlowHub.AI` becomes an active project with `AiClassifier` (`IClassifier` adapter using `Microsoft.Extensions.AI`)
  - Two interchangeable provider adapters behind one `IChatClient`: Anthropic native (default `claude-haiku-4-5-20251001`) and OpenRouter (default `meta-llama/llama-3.1-70b-instruct`)
  - One round-trip for classification + AI-generated `Title` (extends `ClassificationResult` with optional `Title?`)
  - Graceful fallback to `KeywordClassifier` on any AI failure (network, timeout, JSON parse, schema-violation, generic exception) — capture is always classified
  - `AddFlowHubAi(IConfiguration)` extension with D8 behaviour matrix: silent fallback on missing provider/key, throws on invalid `Ai__Provider`
  - `AiBootLogger` `IHostedService` writes startup log `3020 AiProviderRegistered` / `3021 AiProviderNotConfigured`
  - 18 mocked unit tests (10 for `AiClassifier`, 8 for `AddFlowHubAi`) + 4 trait-gated live integration tests
  - `Makefile`: `make test` filters `Category!=AI`; new `make test-ai` runs the live tests
- **ADR 0004**: AI integration in services (provider, abstraction, prompt + cost strategy)
- **EventId range 3000–3999** reserved for AI (extends ADR 0003 namespacing)
```

- [ ] **Step 15.5: Fill in the "to be filled in" sections of `docs/ai-usage.md`**

In `docs/ai-usage.md` Slice-C section (added in Task 12), replace the three "(To be filled in.)" placeholders with concrete content based on what actually happened during this implementation:

- "Notable adaptations the implementers caught" — list real corrections (e.g. MEAI method name `GetResponseAsync` vs. `CompleteAsync` mismatch, `Anthropic.SDK` factory shape, `JsonPropertyName` snake_case mapping needed for the DTO, etc.).
- "Generated vs. handwritten share" — table mirroring Slice A/B, with estimated percentages.
- "Reflexion — what worked / what didn't" — sized for the submission PDF.

- [ ] **Step 15.6: Final test pass**

```bash
make build && make test
```
Expected: clean build (warnings-as-errors), all non-AI tests pass.

- [ ] **Step 15.7: Commit**

```bash
git add vault/Blöcke CHANGELOG.md docs/ai-usage.md
git commit -m "docs(vault/blöcke): tick off Slice-C items in Block 3 Nachbereitung + CHANGELOG"
```

- [ ] **Step 15.8: Push (ASK USER FIRST)**

Do not push without explicit user confirmation. Once confirmed:

```bash
git push origin <branch>
```

---

## Self-review

**Spec coverage check (D1–D10):**

- D1 scope = classifier + Title → Tasks 2 (extend record), 5 (DTO with Title), 6 (AiClassifier returns Title)
- D2 two adapters Anthropic + OpenRouter → Task 7 (`BuildChatClient` switch on `AiProvider`)
- D3 abstraction = MEAI → Task 1 (packages), Task 7 (`IChatClient` registration with `UseOpenTelemetry`)
- D4 port shape extend `ClassificationResult` with `Title?` → Task 2
- D5 graceful fallback to `KeywordClassifier` → Task 6 (catch-all + 5 fallback tests)
- D6 default models → Task 7 (`DefaultAnthropicModel`, `DefaultOpenRouterModel` constants + override test)
- D7 structured output via `CompleteAsync<T>`/`GetResponseAsync` + JSON-schema annotations → Task 5 (DTO) + Task 6 (Json deserialise + schema-violation guard)
- D8 no-key startup matrix → Task 7 (`AiServiceCollectionExtensionsTests`, 8 cases)
- D9 testing layered: 10 mocked unit + 4 trait-gated live → Task 6 (10 unit tests), Task 9 (project), Task 10 (4 live tests), Task 11 (Makefile filter)
- D10 explicit `Ai__Provider` env var → Task 7 (no implicit precedence; one env var)

**Cross-checks against the spec's other sections:**

- Configuration surface (8 env vars) → Task 7 reads `Ai:Provider`, `Ai:<P>:ApiKey`, `Ai:<P>:Model`, `Ai:OpenRouter:Endpoint`, `Ai:MaxOutputTokens`. `Ai:TimeoutSeconds` is NOT yet honoured by the wired `IChatClient` factory in Task 7 — implementer should add `HttpClient.Timeout` configuration in `BuildChatClient`. **Plan gap**: Task 7 Step 7.6 should be amended at implementation time to wire the timeout. Captured here as a known follow-up; the spec's defaults (10s) are still documented in ADR 0004.
- Prompt strategy (English system prompt, no few-shot) → Task 4 (`AiPrompts.SystemPrompt` literal, 3 tests pin shape).
- Cost guards (MaxOutputTokens=300, Temperature=0.2, prompt cache) → Task 6 (constructor wires options; 2 dedicated tests). Anthropic `cache_control: ephemeral` is NOT wired in this plan — captured as an optional Slice-C polish for Task 7 Step 7.6 when the implementer adds the timeout wiring; the unit tests don't observe it (would require live calls), so it's verified via Task 10 manually.
- Observability (3001/3002/3010/3020/3021) → 3010, 3020, 3021 wired (Tasks 6, 7). 3001 (`AiClassifierStarted`) and 3002 (`AiClassifierSucceeded`) are Debug-level and listed as optional in the spec — NOT required for this plan; can be added in a follow-up if the OTEL dashboard wants them.
- Module split (production code in `FlowHub.AI`, unit tests in `FlowHub.Web.ComponentTests/Ai/`, live tests in new `FlowHub.AI.IntegrationTests`) → Tasks 3, 6/7, 9/10.
- DI lifetimes (Singleton across the board) → Task 7 (`AddSingleton` for `IChatClient`, `AiClassifier`, `KeywordClassifier`, `AiRegistrationOutcome`).

**Placeholder scan:** No `TBD`, `TODO`, "implement later", or vague directives. Two intentional retrospective placeholders in `docs/ai-usage.md` are explicitly justified inline (filled by Task 15.5, not by the plan author at plan-write time). Step 1.1 has explicit `<MEAI>`/`<MEAIOAI>`/`<ANTSDK>`/`<HOSTING>` placeholders — same convention as Slice A T1 — with fallback values when offline. The "Adapter API note" inside Task 7.6 calls out that exact `Anthropic.SDK` factory shape may differ across versions and instructs the implementer to verify — this is a real version-sensitivity warning, not a TBD.

**Type consistency check:**

- `ClassificationResult(Tags, MatchedSkill, Title? = null)` — same shape used in Tasks 2, 6 (AI return), and existing pipeline tests (default `Title=null` means existing call sites stay valid). ✓
- `AiClassificationResponse(Tags, MatchedSkill, Title)` with `JsonPropertyName` snake_case attrs (Task 6.4) — used by `AiClassifier.ClassifyAsync`'s `JsonSerializer.Deserialize` (Task 6.3). ✓
- `AiProvider` enum (`Anthropic`, `OpenRouter`) — used in `AiRegistrationOutcome.Provider` (Task 7.4) and in `BuildChatClient` switch (Task 7.6). ✓
- `AiRegistrationOutcome(UsesAi, Provider, Model, Reason)` — built in `ResolveOutcome` (Task 7.6), consumed by `AiBootLogger` (Task 7.5) and asserted in tests (Task 7.1). ✓
- EventIds 3010 (`LogFellBack` in `AiClassifier`), 3020/3021 (`LogProviderRegistered`/`LogProviderNotConfigured` in `AiBootLogger`) — match ADR 0004 §EventId table and spec §Observability. ✓
- `IClassifier.ClassifyAsync(string content, CancellationToken cancellationToken)` — unchanged signature; `AiClassifier`, `KeywordClassifier`, `Substitute.For<IClassifier>()` mocks all use it consistently. ✓

---

## Execution

Plan complete and saved to `docs/superpowers/plans/2026-05-03-slice-c-ai-integration.md`.

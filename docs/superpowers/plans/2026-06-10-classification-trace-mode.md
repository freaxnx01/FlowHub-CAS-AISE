# Classification Trace Mode Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Capture per-capture classification telemetry (which classifier ran, latency, tokens) and surface it — plus an estimated cost — in a debug panel on the capture detail page, gated by an environment variable enabled on the demo VPS.

**Architecture:** Each classifier returns a `ClassifierTrace` alongside its `ClassificationResult`; the trace is persisted on the capture via a nullable EF owned entity (mirroring `Attachment`). The detail page renders a panel from the persisted trace when `Demo:Trace:Enabled` is on, computing cost at render via a config-backed price map. Data is always captured; the env var only toggles the panel.

**Tech Stack:** .NET 10, Microsoft.Extensions.AI (`IChatClient`, `ChatResponse.Usage`), EF Core + PostgreSQL (Testcontainers), MudBlazor + bUnit, xUnit + FluentAssertions + NSubstitute.

**Spec:** `docs/superpowers/specs/2026-06-10-classification-trace-mode-design.md`

---

## File Structure

**Phase 1 — domain + capture (Core, AI):**
- Create `source/FlowHub.Core/Classification/ClassifierTrace.cs` — `ClassifierTrace` record + `ClassifierKind` enum.
- Modify `source/FlowHub.Core/Classification/ClassificationResult.cs` — add trailing optional `ClassifierTrace? Trace`.
- Modify `source/FlowHub.Core/Classification/KeywordClassifier.cs` — stopwatch + keyword trace.
- Create `source/FlowHub.AI/AiModelInfo.cs` — provider+model record injected into `AiClassifier`.
- Modify `source/FlowHub.AI/AiClassifier.cs` — build AI trace from usage + model info.
- Modify `source/FlowHub.AI/AiServiceCollectionExtensions.cs` — register `AiModelInfo`, pass to `AiClassifier`.
- Modify `source/FlowHub.Core/Captures/Capture.cs` — add `ClassifierTrace? ClassifierTrace`.

**Phase 2 — persistence:**
- Modify `source/FlowHub.Core/Captures/ICaptureService.cs` — `MarkClassifiedAsync` gains `ClassifierTrace? trace = null`.
- Modify `source/FlowHub.Persistence/EfCaptureService.cs` + `source/FlowHub.Web/Stubs/CaptureServiceStub.cs` — persist trace.
- Modify `source/FlowHub.Web/Pipeline/CaptureEnrichmentConsumer.cs` — pass `result.Trace`.
- Modify `source/FlowHub.Persistence/Entities/CaptureEntity.cs` + `ClassifierTraceOwned.cs` (new) + `CaptureEntityTypeConfiguration.cs` + `Repositories/EfCaptureRepository.cs` — owned entity + mapping.
- New EF migration under `source/FlowHub.Persistence/Migrations/`.

**Phase 3 — cost estimation (Core interface, AI impl):**
- Create `source/FlowHub.Core/Classification/IClassificationCostEstimator.cs`.
- Create `source/FlowHub.AI/Pricing/ClassificationPricingOptions.cs` + `ClassificationCostEstimator.cs`.
- Modify `source/FlowHub.AI/AiServiceCollectionExtensions.cs` — register estimator + options.

**Phase 4 — UI + gate:**
- Create `source/FlowHub.Web/Demo/DemoTraceOptions.cs`.
- Modify `source/FlowHub.Web/Program.cs` — bind options.
- Create `source/FlowHub.Web/Components/Shared/ClassifierTracePanel.razor` (+ `.razor.cs` if needed).
- Modify `source/FlowHub.Web/Components/Pages/CaptureDetail.razor` + `.razor.cs` — gate + cost + panel.
- Modify `demo/docker-compose.yml` — `Demo__Trace__Enabled: "true"`.

---

## Phase 1 — Domain + classifier trace capture

### Task 1: `ClassifierTrace` type + add to `ClassificationResult`

**Files:**
- Create: `source/FlowHub.Core/Classification/ClassifierTrace.cs`
- Modify: `source/FlowHub.Core/Classification/ClassificationResult.cs`

- [ ] **Step 1: Create the trace type**

```csharp
namespace FlowHub.Core.Classification;

/// <summary>Which classifier produced a result.</summary>
public enum ClassifierKind
{
    Ai,
    Keyword,
}

/// <summary>
/// Telemetry from a single classification call, surfaced by the debug/trace mode.
/// <paramref name="Provider"/>, <paramref name="Model"/>, <paramref name="PromptTokens"/>
/// and <paramref name="CompletionTokens"/> are populated only for <see cref="ClassifierKind.Ai"/>.
/// </summary>
public sealed record ClassifierTrace(
    ClassifierKind Kind,
    int LatencyMs,
    string? Provider = null,
    string? Model = null,
    int? PromptTokens = null,
    int? CompletionTokens = null);
```

- [ ] **Step 2: Add `Trace` to `ClassificationResult`**

Append a trailing optional parameter so existing positional construction sites keep compiling:

```csharp
public sealed record ClassificationResult(
    IReadOnlyList<string> Tags,
    string MatchedSkill,
    string? Title = null,
    string? VikunjaProject = null,
    IReadOnlyDictionary<string, string>? Entities = null,
    ClassifierTrace? Trace = null);
```

- [ ] **Step 3: Build**

Run: `dotnet build source/FlowHub.Core`
Expected: Build succeeded, 0 warnings.

- [ ] **Step 4: Commit**

```bash
git add source/FlowHub.Core/Classification/ClassifierTrace.cs source/FlowHub.Core/Classification/ClassificationResult.cs
git commit -m "feat(core): add ClassifierTrace to classification result"
```

---

### Task 2: `KeywordClassifier` populates a keyword trace

**Files:**
- Modify: `source/FlowHub.Core/Classification/KeywordClassifier.cs`
- Test: `tests/FlowHub.Core.Tests/Classification/KeywordClassifierTests.cs` (create if absent; check the dir first)

- [ ] **Step 1: Write the failing test**

```csharp
using FlowHub.Core.Classification;

namespace FlowHub.Core.Tests.Classification;

public sealed class KeywordClassifierTraceTests
{
    [Fact]
    public async Task ClassifyAsync_SetsKeywordTrace_WithNoTokens()
    {
        var sut = new KeywordClassifier();

        var result = await sut.ClassifyAsync("https://example.com", default);

        result.Trace.Should().NotBeNull();
        result.Trace!.Kind.Should().Be(ClassifierKind.Keyword);
        result.Trace.LatencyMs.Should().BeGreaterThanOrEqualTo(0);
        result.Trace.PromptTokens.Should().BeNull();
        result.Trace.CompletionTokens.Should().BeNull();
        result.Trace.Provider.Should().BeNull();
        result.Trace.Model.Should().BeNull();
    }
}
```

(If `tests/FlowHub.Core.Tests/Classification/` has no global usings for Xunit/FluentAssertions, check `tests/FlowHub.Core.Tests/Usings.cs` and add `using Xunit;` / `using FluentAssertions;` as needed to match the project's style.)

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/FlowHub.Core.Tests --filter ClassifyAsync_SetsKeywordTrace_WithNoTokens`
Expected: FAIL — `Trace` is null.

- [ ] **Step 3: Implement**

Rewrite `ClassifyAsync` to time the body and attach a keyword trace to whichever result is produced:

```csharp
    public Task<ClassificationResult> ClassifyAsync(string content, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);
        var sw = System.Diagnostics.Stopwatch.StartNew();

        var result =
            LooksLikeUrl(content) ? new ClassificationResult(["link"], "Wallabag")
            : ContainsTodoKeyword(content) ? new ClassificationResult(["task"], "Vikunja")
            : new ClassificationResult(["unsorted"], string.Empty);

        sw.Stop();
        var traced = result with
        {
            Trace = new ClassifierTrace(ClassifierKind.Keyword, (int)sw.ElapsedMilliseconds),
        };
        return Task.FromResult(traced);
    }
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/FlowHub.Core.Tests --filter ClassifyAsync_SetsKeywordTrace_WithNoTokens`
Expected: PASS

- [ ] **Step 5: Run the full Core.Tests project (no regressions)**

Run: `dotnet test tests/FlowHub.Core.Tests`
Expected: all PASS.

- [ ] **Step 6: Commit**

```bash
git add source/FlowHub.Core/Classification/KeywordClassifier.cs tests/FlowHub.Core.Tests/Classification/KeywordClassifierTraceTests.cs
git commit -m "feat(core): keyword classifier records a trace"
```

---

### Task 3: `AiClassifier` populates an AI trace from usage + model info

**Files:**
- Create: `source/FlowHub.AI/AiModelInfo.cs`
- Modify: `source/FlowHub.AI/AiClassifier.cs`
- Modify: `source/FlowHub.AI/AiServiceCollectionExtensions.cs`
- Test: `tests/FlowHub.AI.IntegrationTests/` OR a unit test project for AI. CHECK FIRST: `AiClassifier` is `internal`; `FlowHub.AI` already has `[assembly: InternalsVisibleTo("FlowHub.Web.ComponentTests")]` (in `AiPrompts.cs`). Put the unit test in `tests/FlowHub.Web.ComponentTests/Classification/AiClassifierTraceTests.cs` so it can see the internal type, OR add an `InternalsVisibleTo` for the AI unit test project if one exists. Inspect existing `AiClassifier` tests first (`grep -rl AiClassifier tests`) and follow that project's location + fake-IChatClient pattern.

- [ ] **Step 1: Create `AiModelInfo`**

```csharp
namespace FlowHub.AI;

/// <summary>Active AI provider + model, injected into <see cref="AiClassifier"/> for trace reporting.</summary>
public sealed record AiModelInfo(string Provider, string Model);
```

- [ ] **Step 2: Write the failing test**

Mirror the existing AiClassifier test setup (fake `IChatClient`). The fake must return a `ChatResponse<AiClassificationResponse>` whose `.Usage` has known token counts. Example shape (adapt to the existing test helpers in the chosen project):

```csharp
[Fact]
public async Task ClassifyAsync_OnSuccess_SetsAiTrace_WithProviderModelAndTokens()
{
    var chat = /* fake IChatClient returning a valid AiClassificationResponse
                  with Usage { InputTokenCount = 123, OutputTokenCount = 45 } */;
    var sut = new AiClassifier(
        chat,
        new KeywordClassifier(),
        NullLogger<AiClassifier>.Instance,
        new ChatOptions { MaxOutputTokens = 300 },
        /* IVikunjaProjectCatalog stub returning {"Inbox":0} */,
        new AiModelInfo("OpenRouter", "google/gemma-4-31b-it:free"));

    var result = await sut.ClassifyAsync("hello world", default);

    result.Trace!.Kind.Should().Be(ClassifierKind.Ai);
    result.Trace.Provider.Should().Be("OpenRouter");
    result.Trace.Model.Should().Be("google/gemma-4-31b-it:free");
    result.Trace.PromptTokens.Should().Be(123);
    result.Trace.CompletionTokens.Should().Be(45);
    result.Trace.LatencyMs.Should().BeGreaterThanOrEqualTo(0);
}

[Fact]
public async Task ClassifyAsync_OnSchemaViolation_ReturnsKeywordTrace()
{
    var chat = /* fake IChatClient returning an INVALID payload (TryGetResult false) */;
    var sut = new AiClassifier(chat, new KeywordClassifier(), NullLogger<AiClassifier>.Instance,
        new ChatOptions(), /* catalog stub */, new AiModelInfo("OpenRouter", "m"));

    var result = await sut.ClassifyAsync("todo: x", default);

    result.Trace!.Kind.Should().Be(ClassifierKind.Keyword);
}
```

> For the fake `IChatClient`: the existing AiClassifier tests already construct responses for `GetResponseAsync<T>`. Reuse their helper. The MEAI type is `ChatResponse<T>` with a settable `Usage` property of type `UsageDetails?` (`UsageDetails { InputTokenCount, OutputTokenCount : long? }`). If the existing helper doesn't set Usage, extend it to accept a `UsageDetails`.

- [ ] **Step 3: Run tests to verify they fail**

Run: `dotnet test <chosen test project> --filter AiClassifierTrace`
Expected: FAIL — `AiModelInfo` ctor param doesn't exist / Trace null.

- [ ] **Step 4: Implement**

Add the `AiModelInfo` dependency and build the trace. In `AiClassifier`:

```csharp
    private readonly IVikunjaProjectCatalog _catalog;
    private readonly AiModelInfo _modelInfo;

    public AiClassifier(
        IChatClient chat,
        IClassifier keyword,
        ILogger<AiClassifier> log,
        ChatOptions options,
        IVikunjaProjectCatalog catalog,
        AiModelInfo modelInfo)
    {
        _chat = chat;
        _keyword = keyword;
        _log = log;
        _options = options;
        _catalog = catalog;
        _modelInfo = modelInfo;
    }
```

In the success path, after validating `payload`, before returning, stop the stopwatch and build the trace:

```csharp
            sw.Stop();
            var trace = new ClassifierTrace(
                ClassifierKind.Ai,
                (int)sw.ElapsedMilliseconds,
                _modelInfo.Provider,
                _modelInfo.Model,
                (int?)response.Usage?.InputTokenCount,
                (int?)response.Usage?.OutputTokenCount);

            return new ClassificationResult(payload.Tags, payload.MatchedSkill, payload.Title, project, entities, trace);
```

The fallback path already returns `await _keyword.ClassifyAsync(...)`, which now carries the keyword trace — leave it unchanged.

- [ ] **Step 5: Register `AiModelInfo` and pass it**

In `AiServiceCollectionExtensions.AddFlowHubAi`, inside the `outcome.UsesAi` branch (where `AiClassifier` is constructed), register and inject:

```csharp
        services.AddSingleton(new AiModelInfo(outcome.Provider!.Value.ToString(), model));

        services.AddSingleton(sp => new AiClassifier(
            sp.GetRequiredService<IChatClient>(),
            sp.GetRequiredService<KeywordClassifier>(),
            sp.GetRequiredService<ILogger<AiClassifier>>(),
            new ChatOptions { MaxOutputTokens = maxTokens, Temperature = 0.2f },
            sp.GetRequiredService<IVikunjaProjectCatalog>(),
            sp.GetRequiredService<AiModelInfo>()));
```

(`model` is the local already computed as `outcome.Model!`.)

- [ ] **Step 6: Run tests to verify they pass**

Run: `dotnet test <chosen test project> --filter AiClassifierTrace`
Expected: PASS. Then run the whole chosen project to confirm no regression in existing AiClassifier tests.

- [ ] **Step 7: Build + commit**

Run: `dotnet build source/FlowHub.AI` → 0 warnings.

```bash
git add source/FlowHub.AI tests/<chosen path>
git commit -m "feat(ai): AiClassifier records provider/model/token trace"
```

---

### Task 4: Add `ClassifierTrace` to the `Capture` domain record

**Files:**
- Modify: `source/FlowHub.Core/Captures/Capture.cs`

- [ ] **Step 1: Add the field (trailing, optional)**

```csharp
public sealed record Capture(
    Guid Id,
    ChannelKind Source,
    string Content,
    DateTimeOffset CreatedAt,
    LifecycleStage Stage,
    string? MatchedSkill,
    string? FailureReason = null,
    string? Title = null,
    string? ExternalRef = null,
    string? VikunjaProject = null,
    string? EnrichmentDescription = null,
    Attachment? Attachment = null,
    FlowHub.Core.Classification.ClassifierTrace? ClassifierTrace = null);
```

- [ ] **Step 2: Build the solution (catch positional-construction breaks)**

Run: `dotnet build FlowHub.slnx`
Expected: Build succeeded, 0 warnings. (Trailing-optional means existing `new Capture(...)` calls keep compiling. If any used positional args past `Attachment`, fix them — but none should.)

- [ ] **Step 3: Commit**

```bash
git add source/FlowHub.Core/Captures/Capture.cs
git commit -m "feat(core): carry ClassifierTrace on Capture"
```

---

## Phase 2 — Persistence

### Task 5: Thread the trace through `MarkClassifiedAsync` + the consumer

**Files:**
- Modify: `source/FlowHub.Core/Captures/ICaptureService.cs`
- Modify: `source/FlowHub.Persistence/EfCaptureService.cs`
- Modify: `source/FlowHub.Web/Stubs/CaptureServiceStub.cs`
- Modify: `source/FlowHub.Web/Pipeline/CaptureEnrichmentConsumer.cs`

- [ ] **Step 1: Extend the interface**

In `ICaptureService.cs`, change `MarkClassifiedAsync` to:

```csharp
    Task MarkClassifiedAsync(Guid id, string matchedSkill, string? title = null, string? vikunjaProject = null, string? enrichmentDescription = null, FlowHub.Core.Classification.ClassifierTrace? trace = null, CancellationToken cancellationToken = default);
```

- [ ] **Step 2: Persist in `EfCaptureService`**

Update the method signature to match and set the trace in the `with`:

```csharp
    public async Task MarkClassifiedAsync(
        Guid id, string matchedSkill, string? title = null, string? vikunjaProject = null, string? enrichmentDescription = null, FlowHub.Core.Classification.ClassifierTrace? trace = null, CancellationToken cancellationToken = default)
    {
        var capture = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Capture {id} not found.");
        await _repository.UpdateAsync(
            capture with
            {
                Stage = LifecycleStage.Classified,
                MatchedSkill = matchedSkill,
                Title = title ?? capture.Title,
                VikunjaProject = vikunjaProject ?? capture.VikunjaProject,
                EnrichmentDescription = enrichmentDescription ?? capture.EnrichmentDescription,
                ClassifierTrace = trace ?? capture.ClassifierTrace,
            },
            cancellationToken);
    }
```

- [ ] **Step 3: Match the stub signature**

In `CaptureServiceStub.cs`, update `MarkClassifiedAsync` to the new signature and set the trace on the in-memory capture (mirror how it sets MatchedSkill/Title today — read the current body first, then add `ClassifierTrace = trace ?? existing.ClassifierTrace` to its `with`/update).

- [ ] **Step 4: Pass the trace from the consumer**

In `source/FlowHub.Web/Pipeline/CaptureEnrichmentConsumer.cs`, the non-attachment path calls `MarkClassifiedAsync`. Add `result.Trace`:

```csharp
        await _captureService.MarkClassifiedAsync(
            msg.CaptureId,
            result.MatchedSkill,
            result.Title,
            project,
            enrichment?.Description,
            result.Trace,
            ct);
```

The attachment short-circuit path (`MarkClassifiedAsync(msg.CaptureId, "Paperless", cancellationToken: ct)`) stays as-is — trace stays null (no classification ran).

- [ ] **Step 5: Build the solution**

Run: `dotnet build FlowHub.slnx`
Expected: 0 warnings. (Persistence of the owned column is wired in Task 6; this task only carries the value in-memory + via the service contract. The EF entity ignores `ClassifierTrace` until Task 6, so the build is clean but the value isn't yet stored.)

- [ ] **Step 6: Commit**

```bash
git add source/FlowHub.Core/Captures/ICaptureService.cs source/FlowHub.Persistence/EfCaptureService.cs source/FlowHub.Web/Stubs/CaptureServiceStub.cs source/FlowHub.Web/Pipeline/CaptureEnrichmentConsumer.cs
git commit -m "feat: thread ClassifierTrace through MarkClassifiedAsync"
```

---

### Task 6: Persist the trace as an EF owned entity (+ migration)

**Files:**
- Modify: `source/FlowHub.Persistence/Entities/CaptureEntity.cs`
- Create: `source/FlowHub.Persistence/Entities/ClassifierTraceOwned.cs`
- Modify: `source/FlowHub.Persistence/Entities/CaptureEntityTypeConfiguration.cs`
- Modify: `source/FlowHub.Persistence/Repositories/EfCaptureRepository.cs`
- Test: `tests/FlowHub.Persistence.Tests/` (find the existing capture round-trip test and add a case)
- New migration via `dotnet ef`.

- [ ] **Step 1: Write the failing round-trip test**

Find the existing repository round-trip test in `tests/FlowHub.Persistence.Tests` (e.g. `EfCaptureRepositoryTests`); mirror its Testcontainers fixture. Add:

```csharp
[Fact]
public async Task UpdateAsync_PersistsAndReadsBack_ClassifierTrace()
{
    // arrange: add a capture, then update with a classified trace
    var capture = /* new Capture(...) added via AddAsync */;
    var traced = capture with
    {
        Stage = LifecycleStage.Classified,
        MatchedSkill = "Vikunja",
        ClassifierTrace = new ClassifierTrace(ClassifierKind.Ai, 1234, "OpenRouter", "gemma:free", 100, 20),
    };

    await _repo.UpdateAsync(traced, default);
    var read = await _repo.GetByIdAsync(capture.Id, default);

    read!.ClassifierTrace.Should().NotBeNull();
    read.ClassifierTrace!.Kind.Should().Be(ClassifierKind.Ai);
    read.ClassifierTrace.LatencyMs.Should().Be(1234);
    read.ClassifierTrace.Provider.Should().Be("OpenRouter");
    read.ClassifierTrace.Model.Should().Be("gemma:free");
    read.ClassifierTrace.PromptTokens.Should().Be(100);
    read.ClassifierTrace.CompletionTokens.Should().Be(20);
}

[Fact]
public async Task UpdateAsync_NullTrace_StaysNull()
{
    var capture = /* new Capture(...) added */;
    await _repo.UpdateAsync(capture with { Stage = LifecycleStage.Classified, MatchedSkill = "Vikunja" }, default);
    var read = await _repo.GetByIdAsync(capture.Id, default);
    read!.ClassifierTrace.Should().BeNull();
}
```

- [ ] **Step 2: Run to verify failure**

Run: `dotnet test tests/FlowHub.Persistence.Tests --filter ClassifierTrace`
Expected: FAIL (mapping not present; `read.ClassifierTrace` is null in the first test). Requires Docker (Testcontainers).

- [ ] **Step 3: Add the owned entity**

Create `source/FlowHub.Persistence/Entities/ClassifierTraceOwned.cs`:

```csharp
namespace FlowHub.Persistence.Entities;

/// <summary>EF owned-entity shape of <see cref="FlowHub.Core.Classification.ClassifierTrace"/>.</summary>
internal sealed class ClassifierTraceOwned
{
    public string Kind { get; set; } = "";
    public int LatencyMs { get; set; }
    public string? Provider { get; set; }
    public string? Model { get; set; }
    public int? PromptTokens { get; set; }
    public int? CompletionTokens { get; set; }
}
```

In `CaptureEntity.cs`, add: `public ClassifierTraceOwned? ClassifierTrace { get; set; }`.

- [ ] **Step 4: Map it in `CaptureEntityTypeConfiguration`**

Add after the `Attachment` `OwnsOne` block:

```csharp
        builder.OwnsOne(c => c.ClassifierTrace, t =>
        {
            t.Property(x => x.Kind).HasColumnName("ClassifierTrace_Kind").HasMaxLength(16);
            t.Property(x => x.LatencyMs).HasColumnName("ClassifierTrace_LatencyMs");
            t.Property(x => x.Provider).HasColumnName("ClassifierTrace_Provider").HasMaxLength(32);
            t.Property(x => x.Model).HasColumnName("ClassifierTrace_Model").HasMaxLength(128);
            t.Property(x => x.PromptTokens).HasColumnName("ClassifierTrace_PromptTokens");
            t.Property(x => x.CompletionTokens).HasColumnName("ClassifierTrace_CompletionTokens");
        });
        builder.Navigation(c => c.ClassifierTrace).IsRequired(false);
```

- [ ] **Step 5: Map domain ⇄ entity in `EfCaptureRepository`**

In `ToDomain`, add the `ClassifierTrace` argument:

```csharp
        ClassifierTrace: e.ClassifierTrace is null
            ? null
            : new FlowHub.Core.Classification.ClassifierTrace(
                Enum.Parse<FlowHub.Core.Classification.ClassifierKind>(e.ClassifierTrace.Kind),
                e.ClassifierTrace.LatencyMs,
                e.ClassifierTrace.Provider,
                e.ClassifierTrace.Model,
                e.ClassifierTrace.PromptTokens,
                e.ClassifierTrace.CompletionTokens));
```

In `ToEntity`, add:

```csharp
        ClassifierTrace = c.ClassifierTrace is null ? null : new ClassifierTraceOwned
        {
            Kind = c.ClassifierTrace.Kind.ToString(),
            LatencyMs = c.ClassifierTrace.LatencyMs,
            Provider = c.ClassifierTrace.Provider,
            Model = c.ClassifierTrace.Model,
            PromptTokens = c.ClassifierTrace.PromptTokens,
            CompletionTokens = c.ClassifierTrace.CompletionTokens,
        },
```

In `UpdateAsync`, persist the owned reference (it loads with the root since owned). Set it explicitly to handle null↔non-null transitions:

```csharp
        entity.ClassifierTrace = capture.ClassifierTrace is null ? null : new ClassifierTraceOwned
        {
            Kind = capture.ClassifierTrace.Kind.ToString(),
            LatencyMs = capture.ClassifierTrace.LatencyMs,
            Provider = capture.ClassifierTrace.Provider,
            Model = capture.ClassifierTrace.Model,
            PromptTokens = capture.ClassifierTrace.PromptTokens,
            CompletionTokens = capture.ClassifierTrace.CompletionTokens,
        };
```

> Note: `GetByIdAsync`/`ListAsync` queries must include the owned entity. EF Core loads owned references automatically with the root (no `.Include` needed) — same as `Attachment` works today. Verify the round-trip test confirms this.

- [ ] **Step 6: Generate the migration**

Run:
```bash
dotnet ef migrations add 0010_AddClassifierTrace \
  --project source/FlowHub.Persistence \
  --startup-project source/FlowHub.Web
```
Expected: a new migration adding the six `ClassifierTrace_*` columns (all nullable). Inspect the generated `Up()` to confirm it only ADDs nullable columns to `Captures` (no data loss, no other table touched). Match the existing migration file-naming convention already in the folder.

- [ ] **Step 7: Run the round-trip tests**

Run: `dotnet test tests/FlowHub.Persistence.Tests --filter ClassifierTrace`
Expected: PASS (both). Then run the full `tests/FlowHub.Persistence.Tests` to confirm no regression.

- [ ] **Step 8: Build + commit**

Run: `dotnet build source/FlowHub.Persistence` → 0 warnings.

```bash
git add source/FlowHub.Persistence tests/FlowHub.Persistence.Tests
git commit -m "feat(persistence): persist ClassifierTrace as owned entity (+migration)"
```

---

## Phase 3 — Cost estimation

### Task 7: `IClassificationCostEstimator` + config-backed implementation

**Files:**
- Create: `source/FlowHub.Core/Classification/IClassificationCostEstimator.cs`
- Create: `source/FlowHub.AI/Pricing/ClassificationPricingOptions.cs`
- Create: `source/FlowHub.AI/Pricing/ClassificationCostEstimator.cs`
- Modify: `source/FlowHub.AI/AiServiceCollectionExtensions.cs`
- Test: `tests/FlowHub.Web.ComponentTests/Pricing/ClassificationCostEstimatorTests.cs` (or wherever AI-facing unit tests live with access — the impl is `public`, so any test project referencing `FlowHub.AI` works; ComponentTests references it).

- [ ] **Step 1: Interface (Core)**

```csharp
namespace FlowHub.Core.Classification;

/// <summary>Estimates the USD cost of a classification call from its token counts.</summary>
public interface IClassificationCostEstimator
{
    /// <returns>Estimated cost in USD, or null when the model is unknown or tokens are unavailable.</returns>
    decimal? Estimate(string? model, int? promptTokens, int? completionTokens);
}
```

- [ ] **Step 2: Options (AI)** — array-shaped to avoid `:` in model ids breaking config keys

```csharp
namespace FlowHub.AI.Pricing;

/// <summary>Bound from <c>Ai:Pricing</c>. Prices are USD per 1,000,000 tokens.</summary>
public sealed class ClassificationPricingOptions
{
    public const string SectionName = "Ai:Pricing";
    public List<ModelPrice> Models { get; set; } = [];
}

public sealed class ModelPrice
{
    public string Model { get; set; } = "";
    public decimal Input { get; set; }
    public decimal Output { get; set; }
}
```

- [ ] **Step 3: Write failing tests**

```csharp
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
        // 1000 prompt * 3/1e6 + 500 completion * 15/1e6 = 0.003 + 0.0075 = 0.0105
        sut.Estimate("m", 1000, 500).Should().Be(0.0105m);
    }

    [Fact]
    public void Estimate_FreeDemoModel_IsZero()
    {
        var sut = Build(); // no config — built-in free model applies
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
}
```

- [ ] **Step 4: Run to verify failure**

Run: `dotnet test tests/FlowHub.Web.ComponentTests --filter ClassificationCostEstimator`
Expected: FAIL — type doesn't exist.

- [ ] **Step 5: Implement**

```csharp
using FlowHub.Core.Classification;
using Microsoft.Extensions.Options;

namespace FlowHub.AI.Pricing;

public sealed class ClassificationCostEstimator : IClassificationCostEstimator
{
    // Built-in: the public demo's free model. All other models are priced via config.
    private const string FreeDemoModel = "google/gemma-4-31b-it:free";

    private readonly IReadOnlyDictionary<string, ModelPrice> _prices;

    public ClassificationCostEstimator(IOptions<ClassificationPricingOptions> options)
    {
        var map = new Dictionary<string, ModelPrice>(StringComparer.Ordinal)
        {
            [FreeDemoModel] = new ModelPrice { Model = FreeDemoModel, Input = 0m, Output = 0m },
        };
        foreach (var price in options.Value.Models)
        {
            if (!string.IsNullOrWhiteSpace(price.Model))
            {
                map[price.Model] = price; // config overrides built-in
            }
        }
        _prices = map;
    }

    public decimal? Estimate(string? model, int? promptTokens, int? completionTokens)
    {
        if (model is null || promptTokens is null || completionTokens is null)
        {
            return null;
        }
        if (!_prices.TryGetValue(model, out var price))
        {
            return null;
        }
        return (promptTokens.Value * price.Input / 1_000_000m)
             + (completionTokens.Value * price.Output / 1_000_000m);
    }
}
```

- [ ] **Step 6: Register in DI**

In `AddFlowHubAi` (always, not only when AI configured — the page may show a trace from a prior run even if AI later unconfigured):

```csharp
        services.Configure<Pricing.ClassificationPricingOptions>(
            configuration.GetSection(Pricing.ClassificationPricingOptions.SectionName));
        services.AddSingleton<IClassificationCostEstimator, Pricing.ClassificationCostEstimator>();
```

Place this near the top of `AddFlowHubAi` (before the `outcome.UsesAi` early-return).

- [ ] **Step 7: Run tests + build**

Run: `dotnet test tests/FlowHub.Web.ComponentTests --filter ClassificationCostEstimator` → PASS.
Run: `dotnet build source/FlowHub.AI` → 0 warnings.

- [ ] **Step 8: Commit**

```bash
git add source/FlowHub.Core/Classification/IClassificationCostEstimator.cs source/FlowHub.AI/Pricing tests/FlowHub.Web.ComponentTests/Pricing source/FlowHub.AI/AiServiceCollectionExtensions.cs
git commit -m "feat(ai): config-backed classification cost estimator"
```

---

## Phase 4 — UI + env gate

### Task 8: `DemoTraceOptions` + binding + compose flag

**Files:**
- Create: `source/FlowHub.Web/Demo/DemoTraceOptions.cs`
- Modify: `source/FlowHub.Web/Program.cs`
- Modify: `demo/docker-compose.yml`

- [ ] **Step 1: Options class** (mirror `DemoNotifyOptions`)

```csharp
namespace FlowHub.Web.Demo;

/// <summary>Bound from <c>Demo:Trace</c>. Gates the classification trace panel.</summary>
public sealed class DemoTraceOptions
{
    public const string SectionName = "Demo:Trace";
    public bool Enabled { get; set; }
}
```

- [ ] **Step 2: Bind in `Program.cs`** (near the other `Demo:*` wiring, ~line 122)

```csharp
builder.Services.Configure<FlowHub.Web.Demo.DemoTraceOptions>(
    builder.Configuration.GetSection(FlowHub.Web.Demo.DemoTraceOptions.SectionName));
```

- [ ] **Step 3: Enable on the demo VPS** — in `demo/docker-compose.yml`, `flowhub.web.environment`, next to the other `Demo__*` keys:

```yaml
      # Classification trace panel — on for the public demo (transparency).
      Demo__Trace__Enabled: "true"
```

- [ ] **Step 4: Build + render check + commit**

Run: `dotnet build source/FlowHub.Web` → 0 warnings.
Run: `Ai__OpenRouter__ApiKey=x docker compose -f docker-compose.yml -f demo/docker-compose.yml config >/dev/null && echo RENDER_OK`

```bash
git add source/FlowHub.Web/Demo/DemoTraceOptions.cs source/FlowHub.Web/Program.cs demo/docker-compose.yml
git commit -m "feat(web): DemoTraceOptions gate + enable on demo VPS"
```

---

### Task 9: `ClassifierTracePanel` component + wire into `CaptureDetail`

**Files:**
- Create: `source/FlowHub.Web/Components/Shared/ClassifierTracePanel.razor`
- Modify: `source/FlowHub.Web/Components/Pages/CaptureDetail.razor`
- Modify: `source/FlowHub.Web/Components/Pages/CaptureDetail.razor.cs`
- Test: `tests/FlowHub.Web.ComponentTests/Components/ClassifierTracePanelTests.cs`

- [ ] **Step 1: Write the failing bUnit tests**

```csharp
using Bunit;
using FlowHub.Core.Classification;
using FlowHub.Web.Components.Shared;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

namespace FlowHub.Web.ComponentTests.Components;

public sealed class ClassifierTracePanelTests : TestContext
{
    public ClassifierTracePanelTests() => Services.AddMudServices();

    [Fact]
    public void Renders_AiTrace_WithModelTokensAndCost()
    {
        var trace = new ClassifierTrace(ClassifierKind.Ai, 1200, "OpenRouter", "gemma:free", 100, 20);
        var cut = RenderComponent<ClassifierTracePanel>(p => p
            .Add(x => x.Trace, trace)
            .Add(x => x.EstimatedCostUsd, 0m));

        cut.Markup.Should().Contain("OpenRouter");
        cut.Markup.Should().Contain("gemma:free");
        cut.Markup.Should().Contain("1200");   // latency ms
        cut.Markup.Should().Contain("100");     // prompt tokens
        cut.Markup.Should().Contain("20");      // completion tokens
    }

    [Fact]
    public void Renders_NoClassificationNote_WhenTraceNull()
    {
        var cut = RenderComponent<ClassifierTracePanel>(p => p
            .Add(x => x.Trace, (ClassifierTrace?)null)
            .Add(x => x.EstimatedCostUsd, (decimal?)null));

        cut.Markup.Should().Contain("without LLM classification");
    }
}
```

> Check an existing bUnit test in `tests/FlowHub.Web.ComponentTests` for the exact base class / MudBlazor service setup (e.g. whether they use `TestContext` + `AddMudServices`, or a shared fixture) and match it.

- [ ] **Step 2: Run to verify failure**

Run: `dotnet test tests/FlowHub.Web.ComponentTests --filter ClassifierTracePanel`
Expected: FAIL — component doesn't exist.

- [ ] **Step 3: Implement the component**

`source/FlowHub.Web/Components/Shared/ClassifierTracePanel.razor`:

```razor
@namespace FlowHub.Web.Components.Shared

@if (Trace is null)
{
    <MudText Typo="Typo.body2" Class="mud-text-secondary">
        <MudIcon Icon="@Icons.Material.Filled.Info" Size="Size.Small" Class="mr-1" />
        Routed without LLM classification (e.g. file upload).
    </MudText>
}
else
{
    <MudPaper Elevation="0" Class="pa-3" Style="background:var(--mud-palette-background-grey);">
        <MudStack Spacing="1">
            <MudChip T="string" Size="Size.Small"
                     Color="@(Trace.Kind == ClassifierKind.Ai ? Color.Primary : Color.Default)">
                @(Trace.Kind == ClassifierKind.Ai ? "AI classifier" : "Keyword/URL fallback")
            </MudChip>
            @if (Trace.Kind == ClassifierKind.Ai)
            {
                <MudText Typo="Typo.body2"><strong>Provider/Model:</strong> @Trace.Provider / @Trace.Model</MudText>
                <MudText Typo="Typo.body2"><strong>Tokens:</strong> @Trace.PromptTokens prompt + @Trace.CompletionTokens completion</MudText>
                <MudText Typo="Typo.body2"><strong>Est. cost:</strong> @FormatCost(EstimatedCostUsd)</MudText>
            }
            <MudText Typo="Typo.body2"><strong>Latency:</strong> @Trace.LatencyMs ms</MudText>
        </MudStack>
    </MudPaper>
}

@code {
    [Parameter] public ClassifierTrace? Trace { get; set; }
    [Parameter] public decimal? EstimatedCostUsd { get; set; }

    private static string FormatCost(decimal? cost) => cost switch
    {
        null => "—",
        0m => "free",
        _ => $"${cost.Value:0.0000}",
    };
}
```

Ensure `FlowHub.Core.Classification` is in `_Imports.razor` or add `@using FlowHub.Core.Classification` to the component.

- [ ] **Step 4: Wire into `CaptureDetail`**

In `CaptureDetail.razor.cs`, inject the gate + estimator and expose computed values:

```csharp
    [Inject] private Microsoft.Extensions.Options.IOptions<FlowHub.Web.Demo.DemoTraceOptions> TraceOptions { get; set; } = default!;
    [Inject] private FlowHub.Core.Classification.IClassificationCostEstimator CostEstimator { get; set; } = default!;

    private bool ShowTrace => TraceOptions.Value.Enabled;
    private decimal? TraceCostUsd =>
        _capture?.ClassifierTrace is { } t
            ? CostEstimator.Estimate(t.Model, t.PromptTokens, t.CompletionTokens)
            : null;
```

In `CaptureDetail.razor`, after the Metadata `</MudStack>` block, add:

```razor
            @if (ShowTrace)
            {
                <MudText Typo="Typo.subtitle2" Class="mb-1 mt-2">
                    <MudIcon Icon="@Icons.Material.Filled.Insights" Size="Size.Small" Class="mr-1" />Classification trace
                </MudText>
                <ClassifierTracePanel Trace="@_capture.ClassifierTrace" EstimatedCostUsd="@TraceCostUsd" />
            }
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test tests/FlowHub.Web.ComponentTests --filter ClassifierTracePanel`
Expected: PASS.

- [ ] **Step 6: Run full ComponentTests (no regression on CaptureDetail tests)**

Run: `dotnet test tests/FlowHub.Web.ComponentTests`
Expected: all PASS. (If `CaptureDetail` has existing bUnit tests, they now need `DemoTraceOptions` + `IClassificationCostEstimator` registered in their test setup — add `Services.Configure<DemoTraceOptions>(...)`/a default-disabled options and a substitute estimator if those tests fail with DI errors. Fix the test setup, not the component.)

- [ ] **Step 7: Build + commit**

Run: `dotnet build source/FlowHub.Web` → 0 warnings.

```bash
git add source/FlowHub.Web/Components tests/FlowHub.Web.ComponentTests/Components
git commit -m "feat(web): classification trace panel on capture detail (gated)"
```

---

## Phase 5 — Gate

### Task 10: Full build + test gate

- [ ] **Step 1: Build the whole solution (warnings = errors)**

Run: `dotnet build FlowHub.slnx`
Expected: Build succeeded, 0 warnings.

- [ ] **Step 2: Run the non-infra suites**

Run: `dotnet test tests/FlowHub.Core.Tests tests/FlowHub.Web.ComponentTests tests/FlowHub.Skills.Tests tests/FlowHub.Skills.ContractTests`
Expected: all green.

- [ ] **Step 3: Run the Persistence suite (Testcontainers; needs Docker)**

Run: `dotnet test tests/FlowHub.Persistence.Tests`
Expected: all green (includes the new trace round-trip).

- [ ] **Step 4: Commit any fixes**

```bash
git commit -am "test: reconcile suite after trace-mode changes"
```

---

## Self-Review Notes (author)

- **Spec coverage:** which-classifier + provider/model (Tasks 1-3), latency (Tasks 2-3), tokens (Task 3), cost (Task 7, displayed Task 9), persist via migration (Task 6), env gate + demo-on (Task 8), panel + no-classification note (Task 9), always-capture/gate-display (Task 5 persists unconditionally; Task 8/9 gate display). All spec sections map to tasks.
- **Refinement vs spec:** built-in price map ships ONLY the free demo model (0/0); other models priced via `Ai:Pricing:Models[]` config; unknown → null/"—". This avoids hardcoding unverified prices and is reflected in Task 7. The spec's "Haiku = real price" becomes "Haiku priced via config if desired."
- **Type consistency:** `ClassifierTrace(Kind, LatencyMs, Provider, Model, PromptTokens, CompletionTokens)` used identically in Tasks 1, 3, 6, 9; `MarkClassifiedAsync(..., ClassifierTrace? trace, CancellationToken)` consistent across Tasks 5; `IClassificationCostEstimator.Estimate(string?, int?, int?)` consistent Tasks 7, 9.
- **Edge case:** attachment captures (no classifier) → null trace → "routed without LLM classification" note (Tasks 5, 9).

# Per-Vikunja-Project Routing & Capture Enrichment — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Route Vikunja-bound captures to specific projects picked by the classifier from a live catalog, and run a pluggable per-bucket enricher (e.g. `QuotesEnricher` adding an AuthorInfo paragraph) before posting to Vikunja.

**Architecture:** Two-pass pipeline — `AiClassifier` returns `{matched_skill, project, entities}`, `EnricherDispatcher` resolves an `IEnricher` by bucket name (no-op if none), `VikunjaSkillIntegration` resolves the bucket → project id via a TTL-cached `IVikunjaProjectCatalog` and posts with the enriched description. All failures degrade gracefully — captures always land somewhere.

**Tech Stack:** .NET 10, Blazor Server, EF Core + Postgres, `Microsoft.Extensions.AI` (Anthropic / OpenRouter), MudBlazor, xUnit + NSubstitute + FluentAssertions, bUnit, Playwright.

**Spec:** `docs/superpowers/specs/2026-05-17-routing-and-enrichment-design.md`

**Note on DB:** the persistence layer uses Postgres (`character varying(64)` columns in migrations), not SQLite — adjust column types accordingly when creating the migration.

---

## File map

**Created**
- `source/FlowHub.Core/Classification/IEnricher.cs`
- `source/FlowHub.Core/Classification/EnrichmentResult.cs`
- `source/FlowHub.Core/Skills/IVikunjaProjectCatalog.cs`
- `source/FlowHub.AI/EnricherDispatcher.cs`
- `source/FlowHub.AI/Enrichers/QuotesEnricher.cs`
- `source/FlowHub.AI/Enrichers/QuotesEnricherPrompts.cs`
- `source/FlowHub.Skills/Vikunja/VikunjaProjectCatalog.cs`
- `source/FlowHub.Persistence/Migrations/<timestamp>_0008_AddVikunjaProjectToCapture.cs` (+ Designer)
- `tests/FlowHub.Web.ComponentTests/Classification/EnricherDispatcherTests.cs`
- `tests/FlowHub.Web.ComponentTests/Classification/QuotesEnricherTests.cs`
- `tests/FlowHub.Web.ComponentTests/Classification/ClassifyAndEnrichPipelineTests.cs`
- `tests/FlowHub.Web.ComponentTests/Skills/VikunjaProjectCatalogTests.cs`

**Modified**
- `source/FlowHub.Core/Classification/ClassificationResult.cs` — add `VikunjaProject`, `Entities`
- `source/FlowHub.Core/Captures/Capture.cs` — add `VikunjaProject`
- `source/FlowHub.AI/AiClassificationResponse.cs` — add `Project`, `Entities`
- `source/FlowHub.AI/AiPrompts.cs` — inject bucket list, ask for project + entities
- `source/FlowHub.AI/AiClassifier.cs` — consume catalog, propagate fields
- `source/FlowHub.AI/AiServiceCollectionExtensions.cs` — wire enrichers + dispatcher
- `source/FlowHub.Skills/Vikunja/VikunjaOptions.cs` — replace `DefaultProjectId` with `Fallback*` + `Catalog`
- `source/FlowHub.Skills/Vikunja/VikunjaSkillIntegration.cs` — resolve project id, accept description
- `source/FlowHub.Skills/SkillsServiceCollectionExtensions.cs` — register catalog
- `source/FlowHub.Persistence/Entities/CaptureEntity.cs` — add `VikunjaProject`
- `source/FlowHub.Persistence/Entities/CaptureEntityTypeConfiguration.cs` — column config
- `source/FlowHub.Persistence/EfCaptureService.cs` — pass `VikunjaProject` through `MarkClassifiedAsync`
- `source/FlowHub.Persistence/Repositories/EfCaptureRepository.cs` — read/write column
- `source/FlowHub.Web/Pipeline/CaptureEnrichmentConsumer.cs` — invoke dispatcher, pass description into skill invocation
- `source/FlowHub.Web/Components/Shared/LifecycleBadge.razor` — show `→ <project>` chip
- `source/FlowHub.Web/appsettings.json` (+ `.Development.json`) — new config schema

---

## Task 1: Extend `ClassificationResult` with `VikunjaProject` + `Entities`

**Files:**
- Modify: `source/FlowHub.Core/Classification/ClassificationResult.cs`
- Test: `tests/FlowHub.Web.ComponentTests/Classification/ClassificationResultTests.cs` (new)

- [ ] **Step 1: Write the failing test**

Create `tests/FlowHub.Web.ComponentTests/Classification/ClassificationResultTests.cs`:

```csharp
using FlowHub.Core.Classification;
using FluentAssertions;
using Xunit;

namespace FlowHub.Web.ComponentTests.Classification;

public class ClassificationResultTests
{
    [Fact]
    public void Constructor_DefaultsVikunjaProjectAndEntitiesToNull()
    {
        var result = new ClassificationResult(["tag"], "Vikunja");

        result.VikunjaProject.Should().BeNull();
        result.Entities.Should().BeNull();
    }

    [Fact]
    public void Constructor_AllowsSettingVikunjaProjectAndEntities()
    {
        var entities = new Dictionary<string, string> { ["author"] = "Richard Gabriel" };

        var result = new ClassificationResult(["quote"], "Vikunja", "title", "Quotes", entities);

        result.VikunjaProject.Should().Be("Quotes");
        result.Entities.Should().ContainKey("author").WhoseValue.Should().Be("Richard Gabriel");
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/FlowHub.Web.ComponentTests --filter ClassificationResultTests`
Expected: FAIL — `ClassificationResult` constructor has no `VikunjaProject`/`Entities` parameters.

- [ ] **Step 3: Implement**

Replace contents of `source/FlowHub.Core/Classification/ClassificationResult.cs`:

```csharp
namespace FlowHub.Core.Classification;

/// <summary>
/// Output of <see cref="IClassifier.ClassifyAsync"/>.
/// Slice B (KeywordClassifier) returns Title=null; Slice C (AiClassifier) populates Title
/// in the same round-trip as Tags + MatchedSkill (per ADR 0004 D4). Block 5 adds
/// VikunjaProject (the bucket name the classifier routed to) and Entities (structured
/// fields extracted by the model for downstream enrichers).
/// </summary>
public sealed record ClassificationResult(
    IReadOnlyList<string> Tags,
    string MatchedSkill,
    string? Title = null,
    string? VikunjaProject = null,
    IReadOnlyDictionary<string, string>? Entities = null);
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/FlowHub.Web.ComponentTests --filter ClassificationResultTests`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add source/FlowHub.Core/Classification/ClassificationResult.cs \
        tests/FlowHub.Web.ComponentTests/Classification/ClassificationResultTests.cs
git commit -m "feat(core): extend ClassificationResult with VikunjaProject and Entities"
```

---

## Task 2: Add `IEnricher` + `EnrichmentResult` types

**Files:**
- Create: `source/FlowHub.Core/Classification/IEnricher.cs`
- Create: `source/FlowHub.Core/Classification/EnrichmentResult.cs`

(No tests — pure interface/record. They get exercised in Tasks 5+.)

- [ ] **Step 1: Create `EnrichmentResult`**

`source/FlowHub.Core/Classification/EnrichmentResult.cs`:

```csharp
namespace FlowHub.Core.Classification;

/// <summary>
/// Output of <see cref="IEnricher.EnrichAsync"/>. A bucket-specific enricher returns
/// a ready-to-use description (markdown), plus any structured fields the bucket cares
/// about (currently unused but kept for future Vikunja custom-field mapping).
/// </summary>
public sealed record EnrichmentResult(
    string Description,
    IReadOnlyDictionary<string, string>? Fields = null);
```

- [ ] **Step 2: Create `IEnricher`**

`source/FlowHub.Core/Classification/IEnricher.cs`:

```csharp
using FlowHub.Core.Captures;

namespace FlowHub.Core.Classification;

/// <summary>
/// Driven port for per-bucket capture enrichment. Implementations are registered by
/// bucket name and invoked by <c>EnricherDispatcher</c> when a Capture's
/// <see cref="ClassificationResult.VikunjaProject"/> matches.
/// </summary>
public interface IEnricher
{
    string BucketName { get; }

    Task<EnrichmentResult?> EnrichAsync(
        Capture capture,
        ClassificationResult classification,
        CancellationToken cancellationToken);
}
```

- [ ] **Step 3: Verify build**

Run: `dotnet build source/FlowHub.Core/FlowHub.Core.csproj`
Expected: succeeds with 0 errors.

- [ ] **Step 4: Commit**

```bash
git add source/FlowHub.Core/Classification/IEnricher.cs \
        source/FlowHub.Core/Classification/EnrichmentResult.cs
git commit -m "feat(core): add IEnricher and EnrichmentResult"
```

---

## Task 3: Extend `Capture` record with `VikunjaProject`

**Files:**
- Modify: `source/FlowHub.Core/Captures/Capture.cs`

- [ ] **Step 1: Update record**

Replace contents of `source/FlowHub.Core/Captures/Capture.cs`:

```csharp
namespace FlowHub.Core.Captures;

/// <summary>
/// The central FlowHub noun. A single piece of incoming content from any
/// channel — URL, text, image reference, voice memo, etc.
/// See Glossary entry "Capture" in the CAS Obsidian vault.
/// </summary>
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
    string? VikunjaProject = null);
```

- [ ] **Step 2: Verify build**

Run: `dotnet build FlowHub.slnx`
Expected: succeeds — all positional callers still compile because new property is optional.

- [ ] **Step 3: Commit**

```bash
git add source/FlowHub.Core/Captures/Capture.cs
git commit -m "feat(core): add VikunjaProject to Capture"
```

---

## Task 4: Add `IVikunjaProjectCatalog` interface

**Files:**
- Create: `source/FlowHub.Core/Skills/IVikunjaProjectCatalog.cs`

- [ ] **Step 1: Create interface**

`source/FlowHub.Core/Skills/IVikunjaProjectCatalog.cs`:

```csharp
namespace FlowHub.Core.Skills;

/// <summary>
/// Driving port that exposes the set of Vikunja projects available as routing
/// targets ("buckets"). The classifier prompt is built from this list and the
/// skill integration uses it to resolve a bucket name to a project id.
/// </summary>
public interface IVikunjaProjectCatalog
{
    /// <summary>
    /// Returns a name → projectId map. Implementations are expected to cache and
    /// degrade gracefully on transient API failures; callers should not retry.
    /// </summary>
    Task<IReadOnlyDictionary<string, int>> GetAsync(CancellationToken cancellationToken);
}
```

- [ ] **Step 2: Verify build**

Run: `dotnet build source/FlowHub.Core/FlowHub.Core.csproj`
Expected: succeeds.

- [ ] **Step 3: Commit**

```bash
git add source/FlowHub.Core/Skills/IVikunjaProjectCatalog.cs
git commit -m "feat(core): add IVikunjaProjectCatalog port"
```

---

## Task 5: Rework `VikunjaOptions`

Replace `DefaultProjectId` with `FallbackProject` / `FallbackProjectId` and a `Catalog` sub-section.

**Files:**
- Modify: `source/FlowHub.Skills/Vikunja/VikunjaOptions.cs`

- [ ] **Step 1: Update options class**

Replace contents of `source/FlowHub.Skills/Vikunja/VikunjaOptions.cs`:

```csharp
namespace FlowHub.Skills.Vikunja;

/// <summary>
/// Bound from configuration section <c>Skills:Vikunja</c>.
/// </summary>
public sealed class VikunjaOptions
{
    public const string SectionName = "Skills:Vikunja";

    public string? BaseUrl { get; set; }
    public string? ApiToken { get; set; }

    /// <summary>Bucket name used when the classifier returns an unknown project
    /// or before the catalog has been fetched.</summary>
    public string FallbackProject { get; set; } = "Inbox";

    /// <summary>Project id used until the first successful catalog fetch.</summary>
    public int FallbackProjectId { get; set; }

    public VikunjaCatalogOptions Catalog { get; set; } = new();
}

public sealed class VikunjaCatalogOptions
{
    public TimeSpan RefreshInterval { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(3);
}
```

- [ ] **Step 2: Update DI wiring**

In `source/FlowHub.Skills/SkillsServiceCollectionExtensions.cs`, replace the `AddVikunja` method:

```csharp
    private static void AddVikunja(IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(VikunjaOptions.SectionName);
        var options = section.Get<VikunjaOptions>() ?? new VikunjaOptions();

        string? reason = null;
        if (string.IsNullOrWhiteSpace(options.BaseUrl)) { reason = "missing-base-url"; }
        else if (string.IsNullOrWhiteSpace(options.ApiToken)) { reason = "missing-api-token"; }
        else if (options.FallbackProjectId <= 0) { reason = "missing-fallback-project-id"; }

        if (reason is not null)
        {
            services.AddSingleton(new SkillsRegistrationOutcome("Vikunja", Registered: false, Reason: reason));
            return;
        }

        services.Configure<VikunjaOptions>(section);
        services.AddHttpClient<VikunjaSkillIntegration>(client =>
        {
            client.BaseAddress = new Uri(options.BaseUrl!);
            client.Timeout = TimeSpan.FromSeconds(10);
        });
        services.AddHttpClient<VikunjaProjectCatalog>(client =>
        {
            client.BaseAddress = new Uri(options.BaseUrl!);
            client.Timeout = options.Catalog.RequestTimeout;
        });
        services.AddSingleton<IVikunjaProjectCatalog>(sp => sp.GetRequiredService<VikunjaProjectCatalog>());
        services.AddSingleton<ISkillIntegration>(sp => sp.GetRequiredService<VikunjaSkillIntegration>());
        services.AddSingleton(new SkillsRegistrationOutcome("Vikunja", Registered: true, Reason: "configured"));
    }
```

(`VikunjaProjectCatalog` is implemented in Task 6 — the compile will only succeed after that. Mark this task complete once Task 6 ships.)

- [ ] **Step 3: Update appsettings**

In `source/FlowHub.Web/appsettings.json` (and `.Development.json` if present), replace the `Skills:Vikunja` block:

```jsonc
"Skills": {
  "Vikunja": {
    "BaseUrl": "",
    "ApiToken": "",
    "FallbackProject": "Inbox",
    "FallbackProjectId": 0,
    "Catalog": {
      "RefreshInterval": "00:05:00",
      "RequestTimeout": "00:00:03"
    }
  }
}
```

Remove any `DefaultProjectId` keys.

- [ ] **Step 4: Commit**

```bash
git add source/FlowHub.Skills/Vikunja/VikunjaOptions.cs \
        source/FlowHub.Skills/SkillsServiceCollectionExtensions.cs \
        source/FlowHub.Web/appsettings.json \
        source/FlowHub.Web/appsettings.Development.json 2>/dev/null
git commit -m "refactor(skills): rework VikunjaOptions for catalog + fallback"
```

(The build will fail until Task 6 — that's expected. Don't push until Task 6 is in.)

---

## Task 6: Implement `VikunjaProjectCatalog`

Live API fetch with TTL cache + last-known fallback.

**Files:**
- Create: `source/FlowHub.Skills/Vikunja/VikunjaProjectCatalog.cs`
- Test: `tests/FlowHub.Web.ComponentTests/Skills/VikunjaProjectCatalogTests.cs`

- [ ] **Step 1: Write the failing test**

Create `tests/FlowHub.Web.ComponentTests/Skills/VikunjaProjectCatalogTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using FlowHub.Skills.Vikunja;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace FlowHub.Web.ComponentTests.Skills;

public class VikunjaProjectCatalogTests
{
    private static VikunjaProjectCatalog Build(HttpMessageHandler handler, VikunjaOptions? opts = null)
    {
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://vikunja.test") };
        var options = Options.Create(opts ?? new VikunjaOptions
        {
            BaseUrl = "https://vikunja.test",
            ApiToken = "tok",
            FallbackProject = "Inbox",
            FallbackProjectId = 1,
        });
        return new VikunjaProjectCatalog(http, options, NullLogger<VikunjaProjectCatalog>.Instance, TimeProvider.System);
    }

    [Fact]
    public async Task GetAsync_ReturnsMapFromApi()
    {
        var handler = new ScriptedHandler(_ => JsonContent.Create(new[]
        {
            new { id = 1, title = "Inbox" },
            new { id = 7, title = "Quotes" },
        }));

        var catalog = Build(handler);

        var map = await catalog.GetAsync(CancellationToken.None);

        map.Should().ContainKey("Inbox").WhoseValue.Should().Be(1);
        map.Should().ContainKey("Quotes").WhoseValue.Should().Be(7);
    }

    [Fact]
    public async Task GetAsync_FirstCallFails_ReturnsFallbackOnly()
    {
        var handler = new ScriptedHandler(_ => throw new HttpRequestException("boom"));

        var catalog = Build(handler);

        var map = await catalog.GetAsync(CancellationToken.None);

        map.Should().HaveCount(1).And.ContainKey("Inbox").WhoseValue.Should().Be(1);
    }

    [Fact]
    public async Task GetAsync_WithinTtl_ReturnsCachedResultWithoutSecondCall()
    {
        var calls = 0;
        var handler = new ScriptedHandler(_ =>
        {
            calls++;
            return JsonContent.Create(new[] { new { id = 1, title = "Inbox" } });
        });
        var catalog = Build(handler);

        await catalog.GetAsync(CancellationToken.None);
        await catalog.GetAsync(CancellationToken.None);

        calls.Should().Be(1);
    }

    private sealed class ScriptedHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpContent> _respond;
        public ScriptedHandler(Func<HttpRequestMessage, HttpContent> respond) => _respond = respond;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = _respond(request) });
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/FlowHub.Web.ComponentTests --filter VikunjaProjectCatalogTests`
Expected: FAIL — `VikunjaProjectCatalog` does not exist.

- [ ] **Step 3: Implement catalog**

Create `source/FlowHub.Skills/Vikunja/VikunjaProjectCatalog.cs`:

```csharp
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FlowHub.Core.Skills;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FlowHub.Skills.Vikunja;

public sealed partial class VikunjaProjectCatalog : IVikunjaProjectCatalog
{
    private readonly HttpClient _http;
    private readonly VikunjaOptions _options;
    private readonly ILogger<VikunjaProjectCatalog> _log;
    private readonly TimeProvider _time;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private IReadOnlyDictionary<string, int>? _cache;
    private DateTimeOffset _fetchedAt;

    public VikunjaProjectCatalog(
        HttpClient http,
        IOptions<VikunjaOptions> options,
        ILogger<VikunjaProjectCatalog> log,
        TimeProvider time)
    {
        _http = http;
        _options = options.Value;
        _log = log;
        _time = time;
    }

    public async Task<IReadOnlyDictionary<string, int>> GetAsync(CancellationToken cancellationToken)
    {
        var now = _time.GetUtcNow();
        if (_cache is not null && now - _fetchedAt < _options.Catalog.RefreshInterval)
        {
            return _cache;
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_cache is not null && now - _fetchedAt < _options.Catalog.RefreshInterval)
            {
                return _cache;
            }

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/projects");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiToken);
                using var response = await _http.SendAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();
                var projects = await response.Content.ReadFromJsonAsync<VikunjaProjectDto[]>(cancellationToken)
                    ?? Array.Empty<VikunjaProjectDto>();

                var map = projects
                    .Where(p => !string.IsNullOrWhiteSpace(p.Title))
                    .GroupBy(p => p.Title!, StringComparer.Ordinal)
                    .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.Ordinal);

                _cache = map;
                _fetchedAt = now;
                return _cache;
            }
            catch (Exception ex)
            {
                if (_cache is not null)
                {
                    LogRefreshFailedKeepingCache(ex.GetType().Name);
                    return _cache;
                }

                LogFirstFetchFailed(ex.GetType().Name);
                var fallback = new Dictionary<string, int>(StringComparer.Ordinal)
                {
                    [_options.FallbackProject] = _options.FallbackProjectId,
                };
                _cache = fallback;
                _fetchedAt = now;
                return fallback;
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    private sealed record VikunjaProjectDto(int Id, string? Title);

    [LoggerMessage(EventId = 3020, Level = LogLevel.Warning,
        Message = "Vikunja catalog first fetch failed (reason={Reason}); using fallback bucket only")]
    private partial void LogFirstFetchFailed(string reason);

    [LoggerMessage(EventId = 3021, Level = LogLevel.Warning,
        Message = "Vikunja catalog refresh failed (reason={Reason}); keeping last-known catalog")]
    private partial void LogRefreshFailedKeepingCache(string reason);
}
```

- [ ] **Step 4: Run tests to verify pass**

Run: `dotnet test tests/FlowHub.Web.ComponentTests --filter VikunjaProjectCatalogTests`
Expected: PASS (3 tests).

- [ ] **Step 5: Commit**

```bash
git add source/FlowHub.Skills/Vikunja/VikunjaProjectCatalog.cs \
        tests/FlowHub.Web.ComponentTests/Skills/VikunjaProjectCatalogTests.cs
git commit -m "feat(skills): add VikunjaProjectCatalog with TTL cache and fallback"
```

---

## Task 7: Add `VikunjaProject` column to `CaptureEntity` + EF migration

**Files:**
- Modify: `source/FlowHub.Persistence/Entities/CaptureEntity.cs`
- Modify: `source/FlowHub.Persistence/Entities/CaptureEntityTypeConfiguration.cs`
- Modify: `source/FlowHub.Persistence/Repositories/EfCaptureRepository.cs`
- Create: migration via `dotnet ef`

- [ ] **Step 1: Update entity**

In `source/FlowHub.Persistence/Entities/CaptureEntity.cs`, add property after `ExternalRef`:

```csharp
    public string? ExternalRef { get; set; }
    public string? VikunjaProject { get; set; }
```

- [ ] **Step 2: Update type configuration**

In `source/FlowHub.Persistence/Entities/CaptureEntityTypeConfiguration.cs`, add inside `Configure`:

```csharp
        builder.Property(c => c.VikunjaProject).HasMaxLength(64);
```

- [ ] **Step 3: Update repository read & write**

In `source/FlowHub.Persistence/Repositories/EfCaptureRepository.cs`:

- After the line `entity.MatchedSkill = capture.MatchedSkill;` (~line 60), add: `entity.VikunjaProject = capture.VikunjaProject;`
- In the `ToCapture` mapper (~line 144) and any LINQ projection at ~line 156, add `VikunjaProject = e.VikunjaProject` / `e.VikunjaProject` so the property round-trips.

- [ ] **Step 4: Generate migration**

Run:

```bash
dotnet ef migrations add 0008_AddVikunjaProjectToCapture \
  --project source/FlowHub.Persistence \
  --startup-project source/FlowHub.Web
```

Expected: creates `source/FlowHub.Persistence/Migrations/<timestamp>_0008_AddVikunjaProjectToCapture.cs` + Designer + updated snapshot. Inspect the generated `Up` to confirm it adds a `character varying(64)` nullable column.

- [ ] **Step 5: Apply migration locally**

Run:

```bash
dotnet ef database update \
  --project source/FlowHub.Persistence \
  --startup-project source/FlowHub.Web
```

Expected: command completes; no errors.

- [ ] **Step 6: Build + verify existing tests still pass**

Run: `dotnet test FlowHub.slnx --filter "FullyQualifiedName~Persistence|FullyQualifiedName~Captures"`
Expected: PASS.

- [ ] **Step 7: Commit**

```bash
git add source/FlowHub.Persistence/Entities/CaptureEntity.cs \
        source/FlowHub.Persistence/Entities/CaptureEntityTypeConfiguration.cs \
        source/FlowHub.Persistence/Repositories/EfCaptureRepository.cs \
        source/FlowHub.Persistence/Migrations/
git commit -m "feat(persistence): persist VikunjaProject on Capture"
```

---

## Task 8: Extend `EfCaptureService.MarkClassifiedAsync` to accept project

**Files:**
- Modify: `source/FlowHub.Persistence/EfCaptureService.cs`
- Modify: `source/FlowHub.Core/Captures/ICaptureService.cs` (if signature lives there — otherwise the interface in `EfCaptureService.cs`)

- [ ] **Step 1: Read the existing method**

Read `source/FlowHub.Persistence/EfCaptureService.cs` around line 51 and locate the `MarkClassifiedAsync` signature in both the interface and the implementation.

- [ ] **Step 2: Add `vikunjaProject` parameter**

Update signature and call to `with` to include `VikunjaProject = vikunjaProject ?? capture.VikunjaProject`. Example body update:

```csharp
public Task MarkClassifiedAsync(
    Guid captureId,
    string matchedSkill,
    string? title,
    string? vikunjaProject,
    CancellationToken cancellationToken) =>
    _repo.UpdateAsync(captureId, capture =>
        capture with
        {
            Stage = LifecycleStage.Classified,
            MatchedSkill = matchedSkill,
            Title = title ?? capture.Title,
            VikunjaProject = vikunjaProject ?? capture.VikunjaProject,
        },
        cancellationToken);
```

Update the interface accordingly.

- [ ] **Step 3: Fix callers**

`grep -rn "MarkClassifiedAsync" source tests | grep -v bin | grep -v obj`

For each call site (most importantly `source/FlowHub.Web/Pipeline/CaptureEnrichmentConsumer.cs`), pass `null` for `vikunjaProject` for now — Task 14 wires the real value through.

- [ ] **Step 4: Verify build & tests**

Run: `dotnet build FlowHub.slnx` then `dotnet test FlowHub.slnx --filter "FullyQualifiedName~Persistence"`
Expected: build succeeds, tests pass.

- [ ] **Step 5: Commit**

```bash
git add source/FlowHub.Persistence/EfCaptureService.cs \
        source/FlowHub.Core/Captures/ICaptureService.cs 2>/dev/null \
        source/FlowHub.Web/Pipeline/CaptureEnrichmentConsumer.cs
git commit -m "feat(persistence): thread VikunjaProject through MarkClassifiedAsync"
```

---

## Task 9: Extend `AiClassificationResponse` schema

**Files:**
- Modify: `source/FlowHub.AI/AiClassificationResponse.cs`

- [ ] **Step 1: Update record**

Replace contents:

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
    string? Title,

    [property: Description("Vikunja project bucket name when matched_skill=Vikunja; null otherwise")]
    [property: JsonPropertyName("project")]
    string? Project,

    [property: Description("Optional structured entities the bucket may consume (e.g. quote, author)")]
    [property: JsonPropertyName("entities")]
    Dictionary<string, string>? Entities);
```

- [ ] **Step 2: Verify build**

Run: `dotnet build source/FlowHub.AI/FlowHub.AI.csproj`
Expected: build fails in `AiClassifier.cs` (constructor mismatch). That gets fixed in Task 11.

- [ ] **Step 3: Commit**

```bash
git add source/FlowHub.AI/AiClassificationResponse.cs
git commit -m "feat(ai): add Project and Entities to classification schema"
```

---

## Task 10: Update `AiPrompts` to inject bucket list

**Files:**
- Modify: `source/FlowHub.AI/AiPrompts.cs`

- [ ] **Step 1: Rewrite prompt**

Replace contents of `source/FlowHub.AI/AiPrompts.cs`:

```csharp
using System.Globalization;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

[assembly: InternalsVisibleTo("FlowHub.Web.ComponentTests")]

namespace FlowHub.AI;

internal static class AiPrompts
{
    internal static string BuildSystemPrompt(IReadOnlyCollection<string> vikunjaBuckets)
    {
        var bucketLine = vikunjaBuckets.Count == 0
            ? "Inbox"
            : string.Join(", ", vikunjaBuckets);

        return string.Create(CultureInfo.InvariantCulture, $$"""
            You classify user-captured snippets for a personal knowledge tool called FlowHub.

            For each capture, return:
            - tags: 1–5 short lowercase tags describing the snippet
            - matched_skill: which downstream skill should handle it. Choose exactly ONE:
                "Wallabag"  – the snippet is a URL or article worth saving for later reading
                "Vikunja"   – the snippet is a task, todo, OR a structured piece of content
                              that belongs in a Vikunja project (quote, movie, book, …)
                ""          – none of the above; it will be marked as Orphan
            - project: when matched_skill="Vikunja", pick the best matching project from
              this list. If unsure, pick "Inbox".
                Available: {{bucketLine}}
              Leave empty otherwise.
            - title: a 3–8 word title summarising the snippet (omit only if the snippet
                     is itself shorter than 8 words)
            - entities: optional structured fields the project may use, e.g.
                Quotes → {"quote": "...", "author": "..."}
                Movies → {"title": "...", "year": "..."}
              Omit if nothing applies.

            Reply ONLY via the structured response schema. Never include explanations.
            """);
    }

    internal static IList<ChatMessage> BuildMessages(string content, IReadOnlyCollection<string> vikunjaBuckets) =>
    [
        new ChatMessage(ChatRole.System, BuildSystemPrompt(vikunjaBuckets)),
        new ChatMessage(ChatRole.User, content),
    ];
}
```

- [ ] **Step 2: Verify build**

Run: `dotnet build source/FlowHub.AI/FlowHub.AI.csproj`
Expected: build still fails in `AiClassifier.cs` (call site uses old signature). Fixed in Task 11.

- [ ] **Step 3: Commit**

```bash
git add source/FlowHub.AI/AiPrompts.cs
git commit -m "feat(ai): inject Vikunja bucket list into classifier prompt"
```

---

## Task 11: Wire `AiClassifier` to catalog + propagate new fields

**Files:**
- Modify: `source/FlowHub.AI/AiClassifier.cs`
- Test: `tests/FlowHub.Web.ComponentTests/Classification/AiClassifierTests.cs` (extend if it exists; otherwise create)

- [ ] **Step 1: Locate existing AiClassifier tests**

Run: `find tests -name 'AiClassifier*' -not -path '*/bin/*' -not -path '*/obj/*'`

If a test file exists, read it to match style. Otherwise create one in Step 2.

- [ ] **Step 2: Write/extend failing tests**

Add to `tests/FlowHub.Web.ComponentTests/Classification/AiClassifierTests.cs`:

```csharp
using FlowHub.AI;
using FlowHub.Core.Classification;
using FlowHub.Core.Skills;
using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace FlowHub.Web.ComponentTests.Classification;

public class AiClassifierTests
{
    [Fact]
    public async Task ClassifyAsync_PropagatesProjectAndEntitiesFromModel()
    {
        var chat = Substitute.For<IChatClient>();
        chat.GetResponseAsync<AiClassificationResponse>(
                Arg.Any<IList<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var response = new ChatResponse<AiClassificationResponse>(
                    new ChatResponse(new ChatMessage(ChatRole.Assistant, "")),
                    typeof(AiClassificationResponse))
                {
                    // helper or factory needed in the project; if not present, use the
                    // existing test factory (TestChatClient) wherever the project keeps it.
                };
                return Task.FromResult(response);
            });

        var keyword = new KeywordClassifier();
        var catalog = Substitute.For<IVikunjaProjectCatalog>();
        catalog.GetAsync(Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, int> { ["Inbox"] = 1, ["Quotes"] = 7 });

        var classifier = new AiClassifier(chat, keyword, NullLogger<AiClassifier>.Instance,
            new ChatOptions(), catalog);

        // Arrange the chat response with a canned AiClassificationResponse via TestChatClient.
        // (Use the same harness pattern existing AiClassifierTests use — adapt to project style.)

        var result = await classifier.ClassifyAsync("\"Unix and C…\" — Richard Gabriel", default);

        result.MatchedSkill.Should().Be("Vikunja");
        result.VikunjaProject.Should().Be("Quotes");
        result.Entities.Should().ContainKey("author").WhoseValue.Should().Be("Richard Gabriel");
    }
}
```

> NOTE: The repo already has a chat-client test harness used by existing classifier tests. Replace the inline `Substitute.For<IChatClient>()` block above with that harness — copy the pattern from the existing test if present. The assertions above are the source of truth for behaviour.

- [ ] **Step 3: Run tests to verify they fail**

Run: `dotnet test tests/FlowHub.Web.ComponentTests --filter AiClassifierTests`
Expected: FAIL — constructor doesn't accept `IVikunjaProjectCatalog`, response has no `Project`/`Entities`.

- [ ] **Step 4: Update `AiClassifier`**

Replace contents of `source/FlowHub.AI/AiClassifier.cs`:

```csharp
using System.Diagnostics;
using FlowHub.Core.Classification;
using FlowHub.Core.Skills;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace FlowHub.AI;

internal sealed partial class AiClassifier : IClassifier
{
    private static readonly string[] AllowedSkills = ["Wallabag", "Vikunja", ""];

    private readonly IChatClient _chat;
    private readonly IClassifier _keyword;
    private readonly ILogger<AiClassifier> _log;
    private readonly ChatOptions _options;
    private readonly IVikunjaProjectCatalog _catalog;

    public AiClassifier(
        IChatClient chat,
        IClassifier keyword,
        ILogger<AiClassifier> log,
        ChatOptions options,
        IVikunjaProjectCatalog catalog)
    {
        _chat = chat;
        _keyword = keyword;
        _log = log;
        _options = options;
        _catalog = catalog;
    }

    public async Task<ClassificationResult> ClassifyAsync(string content, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);
        var sw = Stopwatch.StartNew();

        try
        {
            var catalog = await _catalog.GetAsync(cancellationToken);
            var buckets = catalog.Keys.ToArray();

            var response = await _chat.GetResponseAsync<AiClassificationResponse>(
                AiPrompts.BuildMessages(content, buckets),
                _options,
                cancellationToken: cancellationToken);

            if (!response.TryGetResult(out var payload))
            {
                throw new InvalidOperationException("schema_violation");
            }

            if (Array.IndexOf(AllowedSkills, payload.MatchedSkill) < 0)
            {
                throw new InvalidOperationException("schema_violation");
            }

            var project = string.Equals(payload.MatchedSkill, "Vikunja", StringComparison.Ordinal)
                ? payload.Project
                : null;

            IReadOnlyDictionary<string, string>? entities = payload.Entities is { Count: > 0 }
                ? payload.Entities
                : null;

            return new ClassificationResult(payload.Tags, payload.MatchedSkill, payload.Title, project, entities);
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

- [ ] **Step 5: Update DI registration**

In `source/FlowHub.AI/AiServiceCollectionExtensions.cs`, change the `AiClassifier` factory to also inject the catalog:

```csharp
        services.AddSingleton(sp => new AiClassifier(
            sp.GetRequiredService<IChatClient>(),
            sp.GetRequiredService<KeywordClassifier>(),
            sp.GetRequiredService<ILogger<AiClassifier>>(),
            new ChatOptions { MaxOutputTokens = maxTokens, Temperature = 0.2f },
            sp.GetRequiredService<IVikunjaProjectCatalog>()));
```

- [ ] **Step 6: Run tests**

Run: `dotnet test tests/FlowHub.Web.ComponentTests --filter AiClassifierTests`
Expected: PASS.

- [ ] **Step 7: Run full build**

Run: `dotnet build FlowHub.slnx`
Expected: succeeds.

- [ ] **Step 8: Commit**

```bash
git add source/FlowHub.AI/AiClassifier.cs \
        source/FlowHub.AI/AiServiceCollectionExtensions.cs \
        tests/FlowHub.Web.ComponentTests/Classification/AiClassifierTests.cs
git commit -m "feat(ai): consume bucket catalog and propagate project + entities"
```

---

## Task 12: Implement `EnricherDispatcher`

**Files:**
- Create: `source/FlowHub.AI/EnricherDispatcher.cs`
- Test: `tests/FlowHub.Web.ComponentTests/Classification/EnricherDispatcherTests.cs`

- [ ] **Step 1: Write failing tests**

Create `tests/FlowHub.Web.ComponentTests/Classification/EnricherDispatcherTests.cs`:

```csharp
using FlowHub.AI;
using FlowHub.Core.Captures;
using FlowHub.Core.Classification;
using FlowHub.Core.Skills;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace FlowHub.Web.ComponentTests.Classification;

public class EnricherDispatcherTests
{
    private static Capture Sample() => new(
        Guid.NewGuid(), ChannelKind.Web, "content", DateTimeOffset.UtcNow,
        LifecycleStage.Captured, "Vikunja");

    private static IVikunjaProjectCatalog Catalog(params (string Name, int Id)[] buckets)
    {
        var sub = Substitute.For<IVikunjaProjectCatalog>();
        sub.GetAsync(Arg.Any<CancellationToken>())
            .Returns(buckets.ToDictionary(b => b.Name, b => b.Id));
        return sub;
    }

    [Fact]
    public async Task DispatchAsync_NoEnricherForBucket_ReturnsNullAndUnchangedProject()
    {
        var dispatcher = new EnricherDispatcher(
            Array.Empty<IEnricher>(),
            Catalog(("Quotes", 7)),
            new VikunjaFallback("Inbox", 1),
            NullLogger<EnricherDispatcher>.Instance);

        var classification = new ClassificationResult(["t"], "Vikunja", "title", "Quotes");

        var (project, enrichment) = await dispatcher.DispatchAsync(Sample(), classification, default);

        project.Should().Be("Quotes");
        enrichment.Should().BeNull();
    }

    [Fact]
    public async Task DispatchAsync_UnknownProject_CoercedToFallback()
    {
        var dispatcher = new EnricherDispatcher(
            Array.Empty<IEnricher>(),
            Catalog(("Inbox", 1)),
            new VikunjaFallback("Inbox", 1),
            NullLogger<EnricherDispatcher>.Instance);

        var classification = new ClassificationResult(["t"], "Vikunja", "title", "DoesNotExist");

        var (project, enrichment) = await dispatcher.DispatchAsync(Sample(), classification, default);

        project.Should().Be("Inbox");
        enrichment.Should().BeNull();
    }

    [Fact]
    public async Task DispatchAsync_MatchingEnricher_RunsAndReturnsResult()
    {
        var enricher = Substitute.For<IEnricher>();
        enricher.BucketName.Returns("Quotes");
        enricher.EnrichAsync(Arg.Any<Capture>(), Arg.Any<ClassificationResult>(), Arg.Any<CancellationToken>())
            .Returns(new EnrichmentResult("**desc**"));

        var dispatcher = new EnricherDispatcher(
            new[] { enricher },
            Catalog(("Quotes", 7)),
            new VikunjaFallback("Inbox", 1),
            NullLogger<EnricherDispatcher>.Instance);

        var (project, enrichment) = await dispatcher.DispatchAsync(
            Sample(), new ClassificationResult(["t"], "Vikunja", "title", "Quotes"), default);

        project.Should().Be("Quotes");
        enrichment!.Description.Should().Be("**desc**");
    }

    [Fact]
    public async Task DispatchAsync_EnricherThrows_ReturnsNullAndLogs()
    {
        var enricher = Substitute.For<IEnricher>();
        enricher.BucketName.Returns("Quotes");
        enricher.EnrichAsync(Arg.Any<Capture>(), Arg.Any<ClassificationResult>(), Arg.Any<CancellationToken>())
            .Returns<EnrichmentResult?>(_ => throw new InvalidOperationException("LLM down"));

        var dispatcher = new EnricherDispatcher(
            new[] { enricher },
            Catalog(("Quotes", 7)),
            new VikunjaFallback("Inbox", 1),
            NullLogger<EnricherDispatcher>.Instance);

        var (project, enrichment) = await dispatcher.DispatchAsync(
            Sample(), new ClassificationResult(["t"], "Vikunja", "title", "Quotes"), default);

        project.Should().Be("Quotes");
        enrichment.Should().BeNull();
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/FlowHub.Web.ComponentTests --filter EnricherDispatcherTests`
Expected: FAIL — `EnricherDispatcher` and `VikunjaFallback` do not exist.

- [ ] **Step 3: Implement dispatcher**

Create `source/FlowHub.AI/EnricherDispatcher.cs`:

```csharp
using FlowHub.Core.Captures;
using FlowHub.Core.Classification;
using FlowHub.Core.Skills;
using Microsoft.Extensions.Logging;

namespace FlowHub.AI;

/// <summary>
/// Fallback bucket reference resolved from <c>Skills:Vikunja</c>. Defined here
/// (not in FlowHub.Skills) to keep AI module independent of skills internals.
/// </summary>
public sealed record VikunjaFallback(string Name, int Id);

public sealed partial class EnricherDispatcher
{
    private readonly IReadOnlyDictionary<string, IEnricher> _enrichers;
    private readonly IVikunjaProjectCatalog _catalog;
    private readonly VikunjaFallback _fallback;
    private readonly ILogger<EnricherDispatcher> _log;

    public EnricherDispatcher(
        IEnumerable<IEnricher> enrichers,
        IVikunjaProjectCatalog catalog,
        VikunjaFallback fallback,
        ILogger<EnricherDispatcher> log)
    {
        _enrichers = enrichers
            .GroupBy(e => e.BucketName, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
        _catalog = catalog;
        _fallback = fallback;
        _log = log;
    }

    public async Task<(string? Project, EnrichmentResult? Enrichment)> DispatchAsync(
        Capture capture,
        ClassificationResult classification,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(classification.MatchedSkill, "Vikunja", StringComparison.Ordinal))
        {
            return (null, null);
        }

        var requested = classification.VikunjaProject;
        var catalog = await _catalog.GetAsync(cancellationToken);

        string project;
        if (string.IsNullOrWhiteSpace(requested) || !catalog.ContainsKey(requested))
        {
            if (!string.IsNullOrWhiteSpace(requested))
            {
                LogProjectCoerced(requested, _fallback.Name);
            }
            project = _fallback.Name;
        }
        else
        {
            project = requested;
        }

        if (!_enrichers.TryGetValue(project, out var enricher))
        {
            return (project, null);
        }

        try
        {
            var result = await enricher.EnrichAsync(capture, classification, cancellationToken);
            return (project, result);
        }
        catch (Exception ex)
        {
            LogEnrichmentFailed(project, ex.GetType().Name);
            return (project, null);
        }
    }

    [LoggerMessage(EventId = 3011, Level = LogLevel.Warning,
        Message = "Classifier returned unknown Vikunja project '{Requested}' — coercing to '{Fallback}'")]
    private partial void LogProjectCoerced(string requested, string fallback);

    [LoggerMessage(EventId = 3030, Level = LogLevel.Warning,
        Message = "Enricher for bucket '{Bucket}' failed (reason={Reason}); posting without enrichment")]
    private partial void LogEnrichmentFailed(string bucket, string reason);
}
```

- [ ] **Step 4: Run tests**

Run: `dotnet test tests/FlowHub.Web.ComponentTests --filter EnricherDispatcherTests`
Expected: PASS (4 tests).

- [ ] **Step 5: Commit**

```bash
git add source/FlowHub.AI/EnricherDispatcher.cs \
        tests/FlowHub.Web.ComponentTests/Classification/EnricherDispatcherTests.cs
git commit -m "feat(ai): add EnricherDispatcher with bucket coercion and failure handling"
```

---

## Task 13: Implement `QuotesEnricher`

**Files:**
- Create: `source/FlowHub.AI/Enrichers/QuotesEnricherPrompts.cs`
- Create: `source/FlowHub.AI/Enrichers/QuotesEnricher.cs`
- Test: `tests/FlowHub.Web.ComponentTests/Classification/QuotesEnricherTests.cs`

- [ ] **Step 1: Write failing tests**

Create `tests/FlowHub.Web.ComponentTests/Classification/QuotesEnricherTests.cs`:

```csharp
using FlowHub.AI.Enrichers;
using FlowHub.Core.Captures;
using FlowHub.Core.Classification;
using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace FlowHub.Web.ComponentTests.Classification;

public class QuotesEnricherTests
{
    private static Capture Sample() => new(
        Guid.NewGuid(), ChannelKind.Web,
        "\"Unix and C are the ultimate computer viruses.\", Richard Gabriel",
        DateTimeOffset.UtcNow, LifecycleStage.Captured, "Vikunja");

    private static ClassificationResult Classification(string? author) =>
        new(["quote"], "Vikunja", "Gabriel on Unix and C", "Quotes",
            author is null ? null : new Dictionary<string, string>
            {
                ["quote"] = "Unix and C are the ultimate computer viruses.",
                ["author"] = author,
            });

    [Fact]
    public async Task EnrichAsync_ComposesQuoteAndBioMarkdown()
    {
        var chat = Substitute.For<IChatClient>();
        chat.GetResponseAsync(Arg.Any<IList<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>())
            .Returns(new ChatResponse(new ChatMessage(ChatRole.Assistant,
                "American computer scientist known for work on Lisp.")));

        var enricher = new QuotesEnricher(chat, NullLogger<QuotesEnricher>.Instance);

        var result = await enricher.EnrichAsync(Sample(), Classification("Richard Gabriel"), default);

        result!.Description.Should().Contain("\"Unix and C are the ultimate computer viruses.\"");
        result.Description.Should().Contain("Richard Gabriel");
        result.Description.Should().Contain("American computer scientist");
    }

    [Fact]
    public async Task EnrichAsync_NoAuthor_ReturnsQuoteOnlyDescription()
    {
        var chat = Substitute.For<IChatClient>();
        var enricher = new QuotesEnricher(chat, NullLogger<QuotesEnricher>.Instance);

        var result = await enricher.EnrichAsync(Sample(), Classification(null), default);

        result!.Description.Should().Contain("Unix and C");
        result.Description.Should().NotContain("**About");
        await chat.DidNotReceive().GetResponseAsync(
            Arg.Any<IList<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnrichAsync_EmptyBio_OmitsBioSection()
    {
        var chat = Substitute.For<IChatClient>();
        chat.GetResponseAsync(Arg.Any<IList<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>())
            .Returns(new ChatResponse(new ChatMessage(ChatRole.Assistant, "")));

        var enricher = new QuotesEnricher(chat, NullLogger<QuotesEnricher>.Instance);

        var result = await enricher.EnrichAsync(Sample(), Classification("Unknown Person"), default);

        result!.Description.Should().NotContain("**About");
    }

    [Fact]
    public async Task BucketName_IsQuotes()
    {
        var chat = Substitute.For<IChatClient>();
        var enricher = new QuotesEnricher(chat, NullLogger<QuotesEnricher>.Instance);
        enricher.BucketName.Should().Be("Quotes");
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/FlowHub.Web.ComponentTests --filter QuotesEnricherTests`
Expected: FAIL — types don't exist.

- [ ] **Step 3: Implement prompts**

Create `source/FlowHub.AI/Enrichers/QuotesEnricherPrompts.cs`:

```csharp
using Microsoft.Extensions.AI;

namespace FlowHub.AI.Enrichers;

internal static class QuotesEnricherPrompts
{
    internal const string SystemPrompt =
        "You write a 2–3 sentence factual bio of a public figure for a personal knowledge tool. " +
        "If you don't know the person, reply with an empty string. Never invent facts.";

    internal static IList<ChatMessage> BuildMessages(string author) =>
    [
        new ChatMessage(ChatRole.System, SystemPrompt),
        new ChatMessage(ChatRole.User, author),
    ];
}
```

- [ ] **Step 4: Implement enricher**

Create `source/FlowHub.AI/Enrichers/QuotesEnricher.cs`:

```csharp
using System.Text;
using FlowHub.Core.Captures;
using FlowHub.Core.Classification;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace FlowHub.AI.Enrichers;

public sealed partial class QuotesEnricher : IEnricher
{
    private readonly IChatClient _chat;
    private readonly ILogger<QuotesEnricher> _log;

    public QuotesEnricher(IChatClient chat, ILogger<QuotesEnricher> log)
    {
        _chat = chat;
        _log = log;
    }

    public string BucketName => "Quotes";

    public async Task<EnrichmentResult?> EnrichAsync(
        Capture capture,
        ClassificationResult classification,
        CancellationToken cancellationToken)
    {
        var quote = classification.Entities?.GetValueOrDefault("quote") ?? capture.Content.Trim();
        var author = classification.Entities?.GetValueOrDefault("author");

        var description = new StringBuilder();
        description.Append("> \"").Append(quote.Trim('"', ' ', '\n')).Append('"');
        if (!string.IsNullOrWhiteSpace(author))
        {
            description.Append(" — ").Append(author);
        }
        description.AppendLine().AppendLine();

        if (!string.IsNullOrWhiteSpace(author))
        {
            var bio = await FetchBioAsync(author!, cancellationToken);
            if (!string.IsNullOrWhiteSpace(bio))
            {
                description.Append("**About ").Append(author).Append(":** ").Append(bio.Trim());
            }
        }

        return new EnrichmentResult(description.ToString().TrimEnd());
    }

    private async Task<string?> FetchBioAsync(string author, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _chat.GetResponseAsync(
                QuotesEnricherPrompts.BuildMessages(author),
                new ChatOptions { MaxOutputTokens = 200, Temperature = 0.2f },
                cancellationToken);
            return response.Messages.LastOrDefault()?.Text;
        }
        catch (Exception ex)
        {
            LogBioFetchFailed(author, ex.GetType().Name);
            return null;
        }
    }

    [LoggerMessage(EventId = 3031, Level = LogLevel.Warning,
        Message = "QuotesEnricher bio fetch failed for author='{Author}' (reason={Reason})")]
    private partial void LogBioFetchFailed(string author, string reason);
}
```

- [ ] **Step 5: Run tests**

Run: `dotnet test tests/FlowHub.Web.ComponentTests --filter QuotesEnricherTests`
Expected: PASS (4 tests).

- [ ] **Step 6: Commit**

```bash
git add source/FlowHub.AI/Enrichers/ \
        tests/FlowHub.Web.ComponentTests/Classification/QuotesEnricherTests.cs
git commit -m "feat(ai): add QuotesEnricher with second LLM call for author bio"
```

---

## Task 14: Wire `EnricherDispatcher` + `QuotesEnricher` into DI

**Files:**
- Modify: `source/FlowHub.AI/AiServiceCollectionExtensions.cs`

- [ ] **Step 1: Add registrations**

In `AddFlowHubAi`, after the existing `services.AddSingleton<KeywordClassifier>();`, add:

```csharp
        // Enrichers (always registered — dispatcher is a no-op if no IChatClient bound).
        services.AddSingleton<IEnricher, QuotesEnricher>();
        services.AddSingleton(sp =>
        {
            var section = configuration.GetSection("Skills:Vikunja");
            var fallbackName = section["FallbackProject"] ?? "Inbox";
            var fallbackId = int.TryParse(section["FallbackProjectId"], out var id) ? id : 0;
            return new VikunjaFallback(fallbackName, fallbackId);
        });
        services.AddSingleton<EnricherDispatcher>();
```

Add `using FlowHub.AI.Enrichers;` and `using FlowHub.Core.Classification;` at the top if not already present.

- [ ] **Step 2: Verify build**

Run: `dotnet build FlowHub.slnx`
Expected: succeeds.

- [ ] **Step 3: Commit**

```bash
git add source/FlowHub.AI/AiServiceCollectionExtensions.cs
git commit -m "feat(ai): register EnricherDispatcher and QuotesEnricher"
```

---

## Task 15: Update `VikunjaSkillIntegration` to use catalog + description

**Files:**
- Modify: `source/FlowHub.Skills/Vikunja/VikunjaSkillIntegration.cs`

- [ ] **Step 1: Update integration**

Replace contents of `source/FlowHub.Skills/Vikunja/VikunjaSkillIntegration.cs`:

```csharp
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using FlowHub.Core.Captures;
using FlowHub.Core.Skills;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FlowHub.Skills.Vikunja;

public sealed partial class VikunjaSkillIntegration : ISkillIntegration
{
    private const int FallbackTitleMaxLength = 120;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly VikunjaOptions _options;
    private readonly IVikunjaProjectCatalog _catalog;
    private readonly ILogger<VikunjaSkillIntegration> _log;

    public VikunjaSkillIntegration(
        HttpClient http,
        IOptions<VikunjaOptions> options,
        IVikunjaProjectCatalog catalog,
        ILogger<VikunjaSkillIntegration> log)
    {
        _http = http;
        _options = options.Value;
        _catalog = catalog;
        _log = log;
    }

    public string Name => "Vikunja";

    public async Task<SkillResult> HandleAsync(Capture capture, CancellationToken cancellationToken)
    {
        var title = !string.IsNullOrWhiteSpace(capture.Title)
            ? capture.Title.Trim()
            : Truncate(capture.Content.Trim(), FallbackTitleMaxLength);

        var projectId = await ResolveProjectIdAsync(capture.VikunjaProject, cancellationToken);

        var path = string.Format(
            CultureInfo.InvariantCulture,
            "/api/v1/projects/{0}/tasks",
            projectId);

        var description = capture.FailureReason;  // re-used as enrichment slot — see Task 16
        using var request = new HttpRequestMessage(HttpMethod.Put, path)
        {
            Content = JsonContent.Create(
                description is null ? (object)new { title } : new { title, description },
                options: JsonOptions),
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.ApiToken);

        using var response = await _http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<VikunjaTaskResponse>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Vikunja response body was empty.");

        if (payload.Id is null)
        {
            throw new InvalidOperationException("Vikunja response did not include an 'id' field.");
        }

        return new SkillResult(Success: true, ExternalRef: payload.Id.Value.ToString(CultureInfo.InvariantCulture));
    }

    private async Task<int> ResolveProjectIdAsync(string? projectName, CancellationToken cancellationToken)
    {
        var map = await _catalog.GetAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(projectName) && map.TryGetValue(projectName, out var id))
        {
            return id;
        }

        if (map.TryGetValue(_options.FallbackProject, out var fallbackId))
        {
            return fallbackId;
        }

        return _options.FallbackProjectId;
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max];

    private sealed record VikunjaTaskResponse(long? Id);
}
```

> NOTE: line `var description = capture.FailureReason;` is intentional placeholder plumbing — Task 16 introduces a proper `Description` carrier so the skill doesn't hijack `FailureReason`. Don't ship the branch until Task 16 lands.

- [ ] **Step 2: Verify build (will fail in tests for `VikunjaSkillIntegrationTests` if they exist)**

Run: `dotnet build FlowHub.slnx`
Expected: builds.

- [ ] **Step 3: Commit**

```bash
git add source/FlowHub.Skills/Vikunja/VikunjaSkillIntegration.cs
git commit -m "feat(skills): resolve Vikunja project id via catalog (description plumbing follows)"
```

---

## Task 16: Carry enrichment description through to the skill call

The cleanest carrier is a new field on `Capture`. Add `EnrichmentDescription` (separate from `FailureReason`) and have the consumer populate it before invoking the skill.

**Files:**
- Modify: `source/FlowHub.Core/Captures/Capture.cs`
- Modify: `source/FlowHub.Persistence/Entities/CaptureEntity.cs` (transient — NOT persisted)
- Modify: `source/FlowHub.Skills/Vikunja/VikunjaSkillIntegration.cs`

- [ ] **Step 1: Add `EnrichmentDescription` to `Capture`**

Update `source/FlowHub.Core/Captures/Capture.cs` — add property at the end:

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
    string? EnrichmentDescription = null);
```

This is a transient field — it lives only on the in-memory `Capture` passed to `HandleAsync`. The repository does NOT persist it.

- [ ] **Step 2: Update `VikunjaSkillIntegration`**

In `HandleAsync`, replace the placeholder line `var description = capture.FailureReason;` with:

```csharp
        var description = capture.EnrichmentDescription;
```

- [ ] **Step 3: Verify build**

Run: `dotnet build FlowHub.slnx`
Expected: builds.

- [ ] **Step 4: Commit**

```bash
git add source/FlowHub.Core/Captures/Capture.cs \
        source/FlowHub.Skills/Vikunja/VikunjaSkillIntegration.cs
git commit -m "feat(core): add transient EnrichmentDescription on Capture"
```

---

## Task 17: Invoke dispatcher in `CaptureEnrichmentConsumer`

**Files:**
- Modify: `source/FlowHub.Web/Pipeline/CaptureEnrichmentConsumer.cs`

- [ ] **Step 1: Read current file**

Read `source/FlowHub.Web/Pipeline/CaptureEnrichmentConsumer.cs` and locate:
- where `IClassifier` is invoked
- where `MarkClassifiedAsync` is called
- where the skill (`ISkillIntegration.HandleAsync`) is invoked

- [ ] **Step 2: Inject `EnricherDispatcher`**

Add constructor parameter:

```csharp
private readonly EnricherDispatcher _enricher;
// …
public CaptureEnrichmentConsumer(
    /* existing params */,
    EnricherDispatcher enricher)
{
    /* existing assignments */
    _enricher = enricher;
}
```

- [ ] **Step 3: Wire dispatcher + thread through skill call**

After classifying:

```csharp
var classification = await _classifier.ClassifyAsync(msg.Content, ct);

if (string.IsNullOrEmpty(classification.MatchedSkill))
{
    // existing orphan path
    return;
}

var (project, enrichment) = await _enricher.DispatchAsync(capture, classification, ct);

await _captureService.MarkClassifiedAsync(
    msg.CaptureId,
    classification.MatchedSkill,
    classification.Title,
    project,
    ct);

// When dispatching to the skill, pass an in-memory Capture with enrichment + project set:
var enriched = capture with
{
    MatchedSkill = classification.MatchedSkill,
    Title = classification.Title ?? capture.Title,
    VikunjaProject = project,
    EnrichmentDescription = enrichment?.Description,
};

// (existing) call skill integration with `enriched` instead of `capture`
```

Adapt names to match the file's local variables. The two new behaviours: `_enricher.DispatchAsync` runs after classify; the `Capture` passed to the skill carries `VikunjaProject` + `EnrichmentDescription`.

- [ ] **Step 4: Build & run all tests**

Run: `dotnet build FlowHub.slnx && dotnet test FlowHub.slnx`
Expected: build succeeds; all tests pass.

- [ ] **Step 5: Commit**

```bash
git add source/FlowHub.Web/Pipeline/CaptureEnrichmentConsumer.cs
git commit -m "feat(pipeline): dispatch enricher and thread project/description to skill"
```

---

## Task 18: Show `→ Project` chip on `LifecycleBadge`

**Files:**
- Modify: `source/FlowHub.Web/Components/Shared/LifecycleBadge.razor`
- Test: `tests/FlowHub.Web.ComponentTests/Shared/LifecycleBadgeTests.cs` (extend if exists; otherwise create)

- [ ] **Step 1: Locate current badge**

Read `source/FlowHub.Web/Components/Shared/LifecycleBadge.razor`. Note the existing parameters and how it renders stage / matched skill.

- [ ] **Step 2: Write a failing bUnit test**

Add to (or create) `tests/FlowHub.Web.ComponentTests/Shared/LifecycleBadgeTests.cs`:

```csharp
[Fact]
public void Renders_VikunjaProject_AsArrowChip_WhenSet()
{
    using var ctx = new TestContext();

    var cut = ctx.RenderComponent<LifecycleBadge>(parameters => parameters
        .Add(p => p.Stage, LifecycleStage.Classified)
        .Add(p => p.MatchedSkill, "Vikunja")
        .Add(p => p.VikunjaProject, "Quotes"));

    cut.Markup.Should().Contain("→ Quotes");
}

[Fact]
public void Does_Not_Render_Arrow_When_VikunjaProject_Is_Null()
{
    using var ctx = new TestContext();

    var cut = ctx.RenderComponent<LifecycleBadge>(parameters => parameters
        .Add(p => p.Stage, LifecycleStage.Classified)
        .Add(p => p.MatchedSkill, "Vikunja"));

    cut.Markup.Should().NotContain("→");
}
```

Add `using FlowHub.Core.Captures;`, `using FluentAssertions;`, `using Bunit;`, `using Xunit;`, `using FlowHub.Web.Components.Shared;` as needed.

- [ ] **Step 3: Run tests to verify they fail**

Run: `dotnet test tests/FlowHub.Web.ComponentTests --filter LifecycleBadgeTests`
Expected: FAIL — `VikunjaProject` parameter does not exist.

- [ ] **Step 4: Add parameter + rendering**

In `LifecycleBadge.razor` (or its code-behind), add:

```csharp
[Parameter] public string? VikunjaProject { get; set; }
```

In the markup, after the existing matched-skill chip, add:

```razor
@if (!string.IsNullOrWhiteSpace(VikunjaProject))
{
    <MudChip T="string" Size="Size.Small" Color="Color.Default" Variant="Variant.Text">@($"→ {VikunjaProject}")</MudChip>
}
```

- [ ] **Step 5: Wire the parameter at call sites**

Run: `grep -rn "<LifecycleBadge" source/FlowHub.Web | grep -v bin | grep -v obj`

For each call site that has a `Capture` in scope (e.g. inbox row), add `VikunjaProject="@capture.VikunjaProject"`.

- [ ] **Step 6: Run tests**

Run: `dotnet test tests/FlowHub.Web.ComponentTests --filter LifecycleBadgeTests`
Expected: PASS.

- [ ] **Step 7: Commit**

```bash
git add source/FlowHub.Web/Components/Shared/LifecycleBadge.razor \
        tests/FlowHub.Web.ComponentTests/Shared/LifecycleBadgeTests.cs
git commit -m "feat(web): show → ProjectName chip in LifecycleBadge"
```

---

## Task 19: Integration test — classify + enrich + skill end-to-end

**Files:**
- Create: `tests/FlowHub.Web.ComponentTests/Classification/ClassifyAndEnrichPipelineTests.cs`

- [ ] **Step 1: Write the test**

```csharp
using FlowHub.AI;
using FlowHub.AI.Enrichers;
using FlowHub.Core.Captures;
using FlowHub.Core.Classification;
using FlowHub.Core.Skills;
using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace FlowHub.Web.ComponentTests.Classification;

public class ClassifyAndEnrichPipelineTests
{
    [Fact]
    public async Task RichardGabrielQuote_RoutesToQuotesAndProducesBio()
    {
        // Catalog with both Inbox and Quotes
        var catalog = Substitute.For<IVikunjaProjectCatalog>();
        catalog.GetAsync(Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, int> { ["Inbox"] = 1, ["Quotes"] = 7 });

        // Chat client returns: (call 1) classification JSON, (call 2) bio text.
        var chat = Substitute.For<IChatClient>();
        var classifierResponse = new AiClassificationResponse(
            Tags: ["quote", "computing"],
            MatchedSkill: "Vikunja",
            Title: "Gabriel on Unix and C",
            Project: "Quotes",
            Entities: new Dictionary<string, string>
            {
                ["quote"] = "Unix and C are the ultimate computer viruses.",
                ["author"] = "Richard Gabriel",
            });

        chat.GetResponseAsync<AiClassificationResponse>(
                Arg.Any<IList<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>())
            .Returns(/* use the project's test factory to wrap classifierResponse in ChatResponse<T> */);

        chat.GetResponseAsync(Arg.Any<IList<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>())
            .Returns(new ChatResponse(new ChatMessage(ChatRole.Assistant,
                "American computer scientist; co-author of the 'Worse is Better' essay.")));

        var classifier = new AiClassifier(chat, new KeywordClassifier(),
            NullLogger<AiClassifier>.Instance, new ChatOptions(), catalog);
        var dispatcher = new EnricherDispatcher(
            new IEnricher[] { new QuotesEnricher(chat, NullLogger<QuotesEnricher>.Instance) },
            catalog,
            new VikunjaFallback("Inbox", 1),
            NullLogger<EnricherDispatcher>.Instance);

        var capture = new Capture(Guid.NewGuid(), ChannelKind.Web,
            "\"Unix and C are the ultimate computer viruses.\", Richard Gabriel",
            DateTimeOffset.UtcNow, LifecycleStage.Captured, null);

        var result = await classifier.ClassifyAsync(capture.Content, default);
        var (project, enrichment) = await dispatcher.DispatchAsync(capture, result, default);

        project.Should().Be("Quotes");
        enrichment.Should().NotBeNull();
        enrichment!.Description.Should().Contain("Unix and C")
                                          .And.Contain("Richard Gabriel")
                                          .And.Contain("computer scientist");
    }
}
```

> NOTE: the line `Returns(/* use the project's test factory… */)` must be replaced with the project's existing `ChatResponse<AiClassificationResponse>` factory pattern. Find it via `grep -rn "ChatResponse<AiClassificationResponse>" tests` and reuse.

- [ ] **Step 2: Run the test**

Run: `dotnet test tests/FlowHub.Web.ComponentTests --filter ClassifyAndEnrichPipelineTests`
Expected: PASS.

- [ ] **Step 3: Commit**

```bash
git add tests/FlowHub.Web.ComponentTests/Classification/ClassifyAndEnrichPipelineTests.cs
git commit -m "test(ai): end-to-end classifier+enricher pipeline for Quote example"
```

---

## Task 20: Manual smoke test + final build

- [ ] **Step 1: Run full test suite**

Run: `dotnet test FlowHub.slnx`
Expected: all PASS.

- [ ] **Step 2: Run formatter**

Run: `make format`
Expected: no diff (or trivial whitespace fixes — commit them if so).

- [ ] **Step 3: Boot the app**

Run: `make run`
Open: `http://localhost:5070`. Quick Capture the Richard Gabriel quote. Check the inbox row shows `→ Quotes`. If Vikunja is reachable, verify the task lands in the Quotes project with a description containing the bio.

- [ ] **Step 4: Commit any formatting**

```bash
git add -A && git diff --cached --quiet || git commit -m "chore: dotnet format"
```

---

## Self-review notes

- Every spec requirement maps to a task:
  - Per-project routing → Tasks 1, 5, 6, 7, 8, 9, 10, 11, 15, 17, 18
  - Generic enricher port → Tasks 2, 12, 14
  - Quotes enricher → Tasks 13, 14, 19
  - Best-effort failure handling → built into Tasks 6 (catalog), 11 (classifier), 12 (dispatcher), 13 (enricher)
  - Lifecycle UI badge → Task 18
  - End-to-end Quote test → Task 19
  - VikunjaOptions config schema change → Task 5
- Type consistency check:
  - `ClassificationResult` ctor positional order is consistent across Tasks 1, 11, 12, 13, 19 (`Tags, MatchedSkill, Title, VikunjaProject, Entities`).
  - `IEnricher.EnrichAsync` returns `EnrichmentResult?` — same in Tasks 2, 12, 13.
  - `EnricherDispatcher.DispatchAsync` returns `(string? Project, EnrichmentResult? Enrichment)` — consistent in Tasks 12, 17.
  - `VikunjaFallback` record used in Tasks 12, 14 (DI).
- No placeholders: all tasks include real code, real commands, real expected output. Two tests carry `NOTE` markers about reusing the project's existing chat-response test factory — these are deliberately not invented from thin air to avoid drifting from the existing test-harness style.

---

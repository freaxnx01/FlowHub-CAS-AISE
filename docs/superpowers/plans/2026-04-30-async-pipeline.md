# Async Pipeline Implementation Plan (Block 3 Slice B)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Wire a MassTransit-based async pipeline into FlowHub.Web that decouples capture submit from enrichment + skill routing, with per-consumer retry policies and a fault observer that maps unrecoverable failures to `LifecycleStage.Orphan` / `LifecycleStage.Unhandled`.

**Architecture:** Two events (`CaptureCreated`, `CaptureClassified`) published over MassTransit's in-memory transport in dev/test. Two consumers (`CaptureEnrichmentConsumer`, `SkillRoutingConsumer`) plus one fault observer (`LifecycleFaultObserver`). Classification and integration writes are abstracted behind `IClassifier` and `ISkillIntegration` ports; Slice B ships keyword + logging stubs, Slice C swaps in a real AI classifier.

**Tech Stack:** .NET 10, MassTransit 8.x (in-memory transport), xUnit + FluentAssertions + NSubstitute + MassTransit.Testing.

**Source spec:** `docs/superpowers/specs/2026-04-30-async-pipeline-design.md` (D1–D13).

---

## File Structure

**Create:**
- `source/FlowHub.Core/Events/CaptureCreated.cs`
- `source/FlowHub.Core/Events/CaptureClassified.cs`
- `source/FlowHub.Core/Classification/IClassifier.cs`
- `source/FlowHub.Core/Classification/ClassificationResult.cs`
- `source/FlowHub.Core/Classification/KeywordClassifier.cs`
- `source/FlowHub.Core/Skills/ISkillIntegration.cs`
- `source/FlowHub.Core/Skills/LoggingSkillIntegration.cs`
- `source/FlowHub.Web/Pipeline/CaptureEnrichmentConsumer.cs`
- `source/FlowHub.Web/Pipeline/SkillRoutingConsumer.cs`
- `source/FlowHub.Web/Pipeline/LifecycleFaultObserver.cs`
- `tests/FlowHub.Web.ComponentTests/Pipeline/PipelineTestBase.cs`
- `tests/FlowHub.Web.ComponentTests/Pipeline/CaptureEnrichmentConsumerTests.cs`
- `tests/FlowHub.Web.ComponentTests/Pipeline/SkillRoutingConsumerTests.cs`
- `tests/FlowHub.Web.ComponentTests/Pipeline/LifecycleFaultObserverTests.cs`
- `tests/FlowHub.Web.ComponentTests/Classification/KeywordClassifierTests.cs`
- `docs/adr/0003-async-pipeline.md`
- `docs/ai-usage.md`
- `docker-compose.yml`

**Modify:**
- `Directory.Packages.props` — add MassTransit central versions
- `source/FlowHub.Web/FlowHub.Web.csproj` — reference MassTransit
- `tests/FlowHub.Web.ComponentTests/FlowHub.Web.ComponentTests.csproj` — reference MassTransit.Testing
- `source/FlowHub.Core/Captures/ICaptureService.cs` — add four mark-methods
- `source/FlowHub.Web/Stubs/CaptureServiceStub.cs` — implement mark methods, publish `CaptureCreated` from `SubmitAsync`
- `source/FlowHub.Web/Program.cs` — register MassTransit + classifier + integrations
- `source/FlowHub.Web/appsettings.Development.json` — `Bus:Transport=InMemory`

---

## Task 1: Add MassTransit packages

**Files:**
- Modify: `Directory.Packages.props`
- Modify: `source/FlowHub.Web/FlowHub.Web.csproj`
- Modify: `tests/FlowHub.Web.ComponentTests/FlowHub.Web.ComponentTests.csproj`

- [ ] **Step 1.1: Pick a current MassTransit version**

Run: `dotnet add source/FlowHub.Web package MassTransit --version "8.*"` to discover the current 8.x release, then immediately undo: `dotnet remove source/FlowHub.Web package MassTransit`. Read the version that was resolved (look at the resulting csproj diff before reverting). Use that version below as `<MT_VERSION>`.

If your sandbox doesn't allow network, use `8.3.5` as the placeholder; an executor with internet bumps it to the latest 8.x stable.

- [ ] **Step 1.2: Add central package versions**

In `Directory.Packages.props`, add this `<ItemGroup>` after the existing test ItemGroup:

```xml
  <ItemGroup Label="Messaging">
    <PackageVersion Include="MassTransit" Version="<MT_VERSION>" />
    <PackageVersion Include="MassTransit.RabbitMQ" Version="<MT_VERSION>" />
    <PackageVersion Include="MassTransit.Testing" Version="<MT_VERSION>" />
  </ItemGroup>
```

- [ ] **Step 1.3: Reference MassTransit in FlowHub.Web**

In `source/FlowHub.Web/FlowHub.Web.csproj`, add to the existing `<ItemGroup>` containing `MudBlazor` and `Bogus`:

```xml
    <PackageReference Include="MassTransit" />
    <PackageReference Include="MassTransit.RabbitMQ" />
```

- [ ] **Step 1.4: Reference MassTransit.Testing in the test project**

In `tests/FlowHub.Web.ComponentTests/FlowHub.Web.ComponentTests.csproj`, add to the test `<ItemGroup>`:

```xml
    <PackageReference Include="MassTransit.Testing" />
```

- [ ] **Step 1.5: Restore + build**

Run: `make restore && make build`
Expected: `Build succeeded` with no errors.

- [ ] **Step 1.6: Commit**

```bash
git add Directory.Packages.props source/FlowHub.Web/FlowHub.Web.csproj tests/FlowHub.Web.ComponentTests/FlowHub.Web.ComponentTests.csproj
git commit -m "build(deps): add MassTransit + RabbitMQ + Testing packages"
```

---

## Task 2: Create event records

**Files:**
- Create: `source/FlowHub.Core/Events/CaptureCreated.cs`
- Create: `source/FlowHub.Core/Events/CaptureClassified.cs`

Records have no behaviour, so no test. Build verifies compilation.

- [ ] **Step 2.1: Create `CaptureCreated`**

```csharp
// source/FlowHub.Core/Events/CaptureCreated.cs
using FlowHub.Core.Captures;

namespace FlowHub.Core.Events;

public sealed record CaptureCreated(
    Guid CaptureId,
    string Content,
    ChannelKind Source,
    DateTimeOffset CreatedAt);
```

- [ ] **Step 2.2: Create `CaptureClassified`**

```csharp
// source/FlowHub.Core/Events/CaptureClassified.cs
namespace FlowHub.Core.Events;

public sealed record CaptureClassified(
    Guid CaptureId,
    IReadOnlyList<string> Tags,
    string MatchedSkill,
    DateTimeOffset ClassifiedAt);
```

- [ ] **Step 2.3: Build + commit**

```bash
make build
git add source/FlowHub.Core/Events/
git commit -m "feat(core): add CaptureCreated and CaptureClassified events"
```

---

## Task 3: Create classifier port + result type

**Files:**
- Create: `source/FlowHub.Core/Classification/IClassifier.cs`
- Create: `source/FlowHub.Core/Classification/ClassificationResult.cs`

- [ ] **Step 3.1: Create `ClassificationResult`**

```csharp
// source/FlowHub.Core/Classification/ClassificationResult.cs
namespace FlowHub.Core.Classification;

public sealed record ClassificationResult(
    IReadOnlyList<string> Tags,
    string MatchedSkill);
```

- [ ] **Step 3.2: Create `IClassifier`**

```csharp
// source/FlowHub.Core/Classification/IClassifier.cs
namespace FlowHub.Core.Classification;

/// <summary>
/// Driving port for capture classification.
/// Slice B ships <see cref="KeywordClassifier"/>; Slice C swaps in an AI-backed adapter.
/// </summary>
public interface IClassifier
{
    Task<ClassificationResult> ClassifyAsync(string content, CancellationToken cancellationToken);
}
```

- [ ] **Step 3.3: Build + commit**

```bash
make build
git add source/FlowHub.Core/Classification/IClassifier.cs source/FlowHub.Core/Classification/ClassificationResult.cs
git commit -m "feat(core): add IClassifier port and ClassificationResult"
```

---

## Task 4: TDD `KeywordClassifier`

**Files:**
- Create: `tests/FlowHub.Web.ComponentTests/Classification/KeywordClassifierTests.cs`
- Create: `source/FlowHub.Core/Classification/KeywordClassifier.cs`

- [ ] **Step 4.1: Write failing tests**

```csharp
// tests/FlowHub.Web.ComponentTests/Classification/KeywordClassifierTests.cs
using FlowHub.Core.Classification;
using FluentAssertions;

namespace FlowHub.Web.ComponentTests.Classification;

public sealed class KeywordClassifierTests
{
    private readonly KeywordClassifier _sut = new();

    [Fact]
    public async Task ClassifyAsync_UrlContent_RoutesToWallabag()
    {
        var result = await _sut.ClassifyAsync("https://example.com/article", default);

        result.MatchedSkill.Should().Be("Wallabag");
        result.Tags.Should().ContainSingle().Which.Should().Be("link");
    }

    [Fact]
    public async Task ClassifyAsync_TodoContent_RoutesToVikunja()
    {
        var result = await _sut.ClassifyAsync("todo: buy milk", default);

        result.MatchedSkill.Should().Be("Vikunja");
        result.Tags.Should().ContainSingle().Which.Should().Be("task");
    }

    [Fact]
    public async Task ClassifyAsync_TaskWordCaseInsensitive_RoutesToVikunja()
    {
        var result = await _sut.ClassifyAsync("TASK list for tomorrow", default);

        result.MatchedSkill.Should().Be("Vikunja");
    }

    [Fact]
    public async Task ClassifyAsync_PlainText_ReturnsEmptySkill()
    {
        var result = await _sut.ClassifyAsync("just some random sentence", default);

        result.MatchedSkill.Should().BeEmpty();
        result.Tags.Should().ContainSingle().Which.Should().Be("unsorted");
    }
}
```

- [ ] **Step 4.2: Run tests; expect compile failure (no `KeywordClassifier` yet)**

Run: `dotnet test tests/FlowHub.Web.ComponentTests --filter FullyQualifiedName~KeywordClassifierTests`
Expected: FAIL with "type or namespace name 'KeywordClassifier' could not be found".

- [ ] **Step 4.3: Implement `KeywordClassifier`**

```csharp
// source/FlowHub.Core/Classification/KeywordClassifier.cs
namespace FlowHub.Core.Classification;

/// <summary>
/// Deterministic keyword-based classifier used in Block 3 Slice B.
/// Slice C replaces this with an AI-backed implementation that consumes <see cref="IClassifier"/>.
/// </summary>
public sealed class KeywordClassifier : IClassifier
{
    public Task<ClassificationResult> ClassifyAsync(string content, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (LooksLikeUrl(content))
        {
            return Task.FromResult(new ClassificationResult(["link"], "Wallabag"));
        }

        if (ContainsTodoKeyword(content))
        {
            return Task.FromResult(new ClassificationResult(["task"], "Vikunja"));
        }

        return Task.FromResult(new ClassificationResult(["unsorted"], string.Empty));
    }

    private static bool LooksLikeUrl(string content) =>
        Uri.TryCreate(content.Trim(), UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    private static bool ContainsTodoKeyword(string content) =>
        content.Contains("todo", StringComparison.OrdinalIgnoreCase)
        || content.Contains("task", StringComparison.OrdinalIgnoreCase);
}
```

- [ ] **Step 4.4: Run tests; expect pass**

Run: `dotnet test tests/FlowHub.Web.ComponentTests --filter FullyQualifiedName~KeywordClassifierTests`
Expected: 4 passed.

- [ ] **Step 4.5: Commit**

```bash
git add source/FlowHub.Core/Classification/KeywordClassifier.cs tests/FlowHub.Web.ComponentTests/Classification/
git commit -m "feat(core): add KeywordClassifier with URL/todo heuristics"
```

---

## Task 5: Create skill-integration port + logging stub

**Files:**
- Create: `source/FlowHub.Core/Skills/ISkillIntegration.cs`
- Create: `source/FlowHub.Core/Skills/LoggingSkillIntegration.cs`

`LoggingSkillIntegration` has no logic to test beyond logging — its behaviour is exercised end-to-end through the routing-consumer tests in Task 9.

- [ ] **Step 5.1: Create `ISkillIntegration`**

```csharp
// source/FlowHub.Core/Skills/ISkillIntegration.cs
using FlowHub.Core.Captures;

namespace FlowHub.Core.Skills;

/// <summary>
/// Driven port: writes a Capture to a downstream skill-specific service.
/// Slice B ships <see cref="LoggingSkillIntegration"/> stubs; real adapters
/// (Wallabag, Wekan, Vikunja) land in Block 4/5.
/// </summary>
public interface ISkillIntegration
{
    string Name { get; }

    Task WriteAsync(Capture capture, IReadOnlyList<string> tags, CancellationToken cancellationToken);
}
```

- [ ] **Step 5.2: Create `LoggingSkillIntegration`**

```csharp
// source/FlowHub.Core/Skills/LoggingSkillIntegration.cs
using FlowHub.Core.Captures;
using Microsoft.Extensions.Logging;

namespace FlowHub.Core.Skills;

public sealed class LoggingSkillIntegration : ISkillIntegration
{
    private readonly ILogger<LoggingSkillIntegration> _logger;

    public LoggingSkillIntegration(string name, ILogger<LoggingSkillIntegration> logger)
    {
        Name = name;
        _logger = logger;
    }

    public string Name { get; }

    public Task WriteAsync(Capture capture, IReadOnlyList<string> tags, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Stub integration '{Skill}' would write capture {CaptureId} with tags {Tags}",
            Name, capture.Id, tags);
        return Task.CompletedTask;
    }
}
```

- [ ] **Step 5.3: Add `Microsoft.Extensions.Logging.Abstractions` reference**

`FlowHub.Core` is currently a plain class library with no NuGet packages. Add the abstractions package so the skill stub can depend on `ILogger<T>` without dragging in the full logging framework:

In `Directory.Packages.props`, add inside the first `<ItemGroup>`:

```xml
    <PackageVersion Include="Microsoft.Extensions.Logging.Abstractions" Version="10.0.0" />
```

In `source/FlowHub.Core/FlowHub.Core.csproj`, add an `<ItemGroup>` with:

```xml
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" />
```

(Use whatever 10.x version `dotnet list package` reports for the SDK; bump if needed.)

- [ ] **Step 5.4: Build + commit**

```bash
make build
git add source/FlowHub.Core/Skills/ source/FlowHub.Core/FlowHub.Core.csproj Directory.Packages.props
git commit -m "feat(core): add ISkillIntegration port and LoggingSkillIntegration stub"
```

---

## Task 6: Extend `ICaptureService` with mark methods

**Files:**
- Modify: `source/FlowHub.Core/Captures/ICaptureService.cs`

The interface change forces compile errors in `CaptureServiceStub`, which Task 7 fixes.

- [ ] **Step 6.1: Add four mark methods to the interface**

After the existing `SubmitAsync` line, append:

```csharp
    Task MarkClassifiedAsync(Guid id, string matchedSkill, CancellationToken cancellationToken = default);

    Task MarkRoutedAsync(Guid id, CancellationToken cancellationToken = default);

    Task MarkOrphanAsync(Guid id, string reason, CancellationToken cancellationToken = default);

    Task MarkUnhandledAsync(Guid id, string reason, CancellationToken cancellationToken = default);
```

(Tags are not persisted on `Capture` in Slice B per the spec — they travel only on the bus event.)

- [ ] **Step 6.2: Build, expect failure**

Run: `make build`
Expected: build fails — `CaptureServiceStub does not implement interface member ICaptureService.MarkClassifiedAsync`. This is intentional; Task 7 fixes it.

- [ ] **Step 6.3: Do not commit yet** — Task 7 commits the interface + stub change together to keep the build bisectable.

---

## Task 7: Implement mark methods + publish from `SubmitAsync` (TDD)

**Files:**
- Create: `tests/FlowHub.Web.ComponentTests/Stubs/CaptureServiceStubTests.cs`
- Modify: `source/FlowHub.Web/Stubs/CaptureServiceStub.cs`

- [ ] **Step 7.1: Write failing tests**

```csharp
// tests/FlowHub.Web.ComponentTests/Stubs/CaptureServiceStubTests.cs
using FlowHub.Core.Captures;
using FlowHub.Core.Events;
using FlowHub.Web.Stubs;
using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace FlowHub.Web.ComponentTests.Stubs;

public sealed class CaptureServiceStubTests
{
    [Fact]
    public async Task SubmitAsync_NewContent_PublishesCaptureCreated()
    {
        await using var provider = new ServiceCollection()
            .AddSingleton<ICaptureService, CaptureServiceStub>()
            .AddMassTransitTestHarness()
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var sut = provider.GetRequiredService<ICaptureService>();

        var capture = await sut.SubmitAsync("hello", ChannelKind.Web, default);

        var published = await harness.Published.Any<CaptureCreated>(
            x => x.Context.Message.CaptureId == capture.Id);
        published.Should().BeTrue();
    }

    [Fact]
    public async Task MarkClassifiedAsync_UpdatesCaptureStageAndSkill()
    {
        var sut = new CaptureServiceStub(NoopPublishEndpoint.Instance);
        var capture = await sut.SubmitAsync("hello", ChannelKind.Web, default);

        await sut.MarkClassifiedAsync(capture.Id, "Wallabag", default);

        var updated = await sut.GetByIdAsync(capture.Id, default);
        updated!.Stage.Should().Be(LifecycleStage.Classified);
        updated.MatchedSkill.Should().Be("Wallabag");
    }

    [Fact]
    public async Task MarkRoutedAsync_FlipsToRouted()
    {
        var sut = new CaptureServiceStub(NoopPublishEndpoint.Instance);
        var capture = await sut.SubmitAsync("hello", ChannelKind.Web, default);

        await sut.MarkRoutedAsync(capture.Id, default);

        (await sut.GetByIdAsync(capture.Id, default))!.Stage.Should().Be(LifecycleStage.Routed);
    }

    [Fact]
    public async Task MarkOrphanAsync_FlipsToOrphanWithReason()
    {
        var sut = new CaptureServiceStub(NoopPublishEndpoint.Instance);
        var capture = await sut.SubmitAsync("hello", ChannelKind.Web, default);

        await sut.MarkOrphanAsync(capture.Id, "no skill matched", default);

        var updated = (await sut.GetByIdAsync(capture.Id, default))!;
        updated.Stage.Should().Be(LifecycleStage.Orphan);
        updated.FailureReason.Should().Be("no skill matched");
    }

    [Fact]
    public async Task MarkUnhandledAsync_FlipsToUnhandledWithReason()
    {
        var sut = new CaptureServiceStub(NoopPublishEndpoint.Instance);
        var capture = await sut.SubmitAsync("hello", ChannelKind.Web, default);

        await sut.MarkUnhandledAsync(capture.Id, "integration down", default);

        var updated = (await sut.GetByIdAsync(capture.Id, default))!;
        updated.Stage.Should().Be(LifecycleStage.Unhandled);
        updated.FailureReason.Should().Be("integration down");
    }

    [Fact]
    public async Task MarkClassifiedAsync_UnknownId_Throws()
    {
        var sut = new CaptureServiceStub(NoopPublishEndpoint.Instance);

        var act = () => sut.MarkClassifiedAsync(Guid.NewGuid(), "Wallabag", default);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    private sealed class NoopPublishEndpoint : IPublishEndpoint
    {
        public static readonly NoopPublishEndpoint Instance = new();

        public ConnectHandle ConnectPublishObserver(IPublishObserver observer) => new NoopHandle();

        public Task Publish<T>(T message, CancellationToken cancellationToken = default) where T : class =>
            Task.CompletedTask;

        public Task Publish<T>(T message, IPipe<PublishContext<T>> pipe, CancellationToken cancellationToken = default) where T : class =>
            Task.CompletedTask;

        public Task Publish<T>(T message, IPipe<PublishContext> pipe, CancellationToken cancellationToken = default) where T : class =>
            Task.CompletedTask;

        public Task Publish(object message, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task Publish(object message, Type messageType, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task Publish(object message, IPipe<PublishContext> pipe, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task Publish(object message, Type messageType, IPipe<PublishContext> pipe, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task Publish<T>(object values, CancellationToken cancellationToken = default) where T : class => Task.CompletedTask;

        public Task Publish<T>(object values, IPipe<PublishContext<T>> pipe, CancellationToken cancellationToken = default) where T : class => Task.CompletedTask;

        public Task Publish<T>(object values, IPipe<PublishContext> pipe, CancellationToken cancellationToken = default) where T : class => Task.CompletedTask;

        private sealed class NoopHandle : ConnectHandle
        {
            public void Disconnect() { }
            public void Dispose() { }
        }
    }
}
```

If maintaining `NoopPublishEndpoint` becomes painful, switch all unit tests to use `AddMassTransitTestHarness()` like the first test does. The harness provides a real `IPublishEndpoint`.

- [ ] **Step 7.2: Run tests; expect compile failures (constructor mismatch + missing methods)**

Run: `dotnet test tests/FlowHub.Web.ComponentTests --filter FullyQualifiedName~CaptureServiceStubTests`
Expected: FAIL — constructor expects `IPublishEndpoint`, mark methods don't exist.

- [ ] **Step 7.3: Update `CaptureServiceStub`**

Replace the constructor and add the new methods. Full updated file:

```csharp
// source/FlowHub.Web/Stubs/CaptureServiceStub.cs
using Bogus;
using FlowHub.Core.Captures;
using FlowHub.Core.Events;
using MassTransit;

namespace FlowHub.Web.Stubs;

/// <summary>
/// Bogus-backed in-memory stub for <see cref="ICaptureService"/>.
/// Block 3 Slice B: publishes <see cref="CaptureCreated"/> on submit and exposes
/// state-transition methods used by the pipeline consumers.
/// EF Core-backed implementation lands in Block 4.
/// </summary>
public sealed class CaptureServiceStub : ICaptureService
{
    private static readonly string[] SkillNames =
        ["Movies", "Articles", "Books", "Quotes", "Knowledge", "Homelab", "Belege"];

    private static readonly string[] SampleContent =
    [
        "Inception (2010) — rewatch",
        "https://heise.de/select/ct/2026/5/2532311091092661684",
        "https://galaxus.ch/de/s18/product/eine-kurze-geschichte-der-menschheit",
        "Schmidts Katze — Bezirksch...",
        "https://example.com/weird-thing-that-no-skill-knows",
        "\"Information is the resolution of uncertainty\" — Shannon",
        "AdGuard Home self-host",
        "ct 2026/05 article snippet on opkssh",
        "The Imitation Game — Alan Turing",
        "Galaxus Quittung 2026-04-09",
        "https://jellyfin.org/",
        "Star Trek: Strange New Worlds S03",
    ];

    private readonly List<Capture> _captures;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly Lock _lock = new();

    public CaptureServiceStub(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;

        var rng = new Faker { Random = new Bogus.Randomizer(42) };
        var now = DateTimeOffset.UtcNow;

        _captures = Enumerable.Range(0, 12)
            .Select(i => new Capture(
                Id: rng.Random.Guid(),
                Source: rng.PickRandom<ChannelKind>(),
                Content: SampleContent[i % SampleContent.Length],
                CreatedAt: now.AddMinutes(-(i * rng.Random.Int(2, 8) + rng.Random.Int(0, 3))),
                Stage: PickStage(rng, i),
                MatchedSkill: PickSkill(rng, i),
                FailureReason: PickFailureReason(i)))
            .ToList();
    }

    public Task<Capture?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult(_captures.FirstOrDefault(c => c.Id == id));
        }
    }

    public Task<IReadOnlyList<Capture>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            IReadOnlyList<Capture> all = _captures.OrderByDescending(c => c.CreatedAt).ToList();
            return Task.FromResult(all);
        }
    }

    public Task<IReadOnlyList<Capture>> GetRecentAsync(int count, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            IReadOnlyList<Capture> recent = _captures.OrderByDescending(c => c.CreatedAt).Take(count).ToList();
            return Task.FromResult(recent);
        }
    }

    public Task<FailureCounts> GetFailureCountsAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var orphan = _captures.Count(c => c.Stage == LifecycleStage.Orphan);
            var unhandled = _captures.Count(c => c.Stage == LifecycleStage.Unhandled);
            return Task.FromResult(new FailureCounts(orphan, unhandled));
        }
    }

    public async Task<Capture> SubmitAsync(string content, ChannelKind source, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        var capture = new Capture(
            Id: Guid.NewGuid(),
            Source: source,
            Content: content,
            CreatedAt: DateTimeOffset.UtcNow,
            Stage: LifecycleStage.Raw,
            MatchedSkill: null);

        lock (_lock)
        {
            _captures.Add(capture);
        }

        await _publishEndpoint.Publish(
            new CaptureCreated(capture.Id, capture.Content, capture.Source, capture.CreatedAt),
            cancellationToken);

        return capture;
    }

    public Task MarkClassifiedAsync(Guid id, string matchedSkill, CancellationToken cancellationToken = default) =>
        ReplaceCapture(id, c => c with { Stage = LifecycleStage.Classified, MatchedSkill = matchedSkill });

    public Task MarkRoutedAsync(Guid id, CancellationToken cancellationToken = default) =>
        ReplaceCapture(id, c => c with { Stage = LifecycleStage.Routed });

    public Task MarkOrphanAsync(Guid id, string reason, CancellationToken cancellationToken = default) =>
        ReplaceCapture(id, c => c with { Stage = LifecycleStage.Orphan, FailureReason = reason });

    public Task MarkUnhandledAsync(Guid id, string reason, CancellationToken cancellationToken = default) =>
        ReplaceCapture(id, c => c with { Stage = LifecycleStage.Unhandled, FailureReason = reason });

    private Task ReplaceCapture(Guid id, Func<Capture, Capture> transform)
    {
        lock (_lock)
        {
            var index = _captures.FindIndex(c => c.Id == id);
            if (index < 0)
            {
                throw new KeyNotFoundException($"Capture {id} not found.");
            }
            _captures[index] = transform(_captures[index]);
        }
        return Task.CompletedTask;
    }

    private static LifecycleStage PickStage(Faker rng, int index) => index switch
    {
        2 or 8 => LifecycleStage.Orphan,
        4 => LifecycleStage.Unhandled,
        6 => LifecycleStage.Routed,
        _ => LifecycleStage.Completed,
    };

    private static string? PickSkill(Faker rng, int index)
    {
        if (index == 4)
        {
            return null;
        }
        return SkillNames[index % SkillNames.Length];
    }

    private static string? PickFailureReason(int index) => index switch
    {
        2 => "Wallabag API returned 503 Service Unavailable — the Integration was unreachable.",
        8 => "Vikunja write timed out after 30 s — the list could not be updated.",
        _ => null,
    };
}
```

- [ ] **Step 7.4: Run tests; expect pass**

Run: `dotnet test tests/FlowHub.Web.ComponentTests --filter FullyQualifiedName~CaptureServiceStubTests`
Expected: 6 passed.

- [ ] **Step 7.5: Commit**

```bash
git add source/FlowHub.Core/Captures/ICaptureService.cs source/FlowHub.Web/Stubs/CaptureServiceStub.cs tests/FlowHub.Web.ComponentTests/Stubs/
git commit -m "feat(core): publish CaptureCreated on submit; add mark-methods to ICaptureService"
```

---

## Task 8: TDD `CaptureEnrichmentConsumer`

**Files:**
- Create: `tests/FlowHub.Web.ComponentTests/Pipeline/PipelineTestBase.cs`
- Create: `tests/FlowHub.Web.ComponentTests/Pipeline/CaptureEnrichmentConsumerTests.cs`
- Create: `source/FlowHub.Web/Pipeline/CaptureEnrichmentConsumer.cs`

- [ ] **Step 8.1: Create the shared test harness base**

```csharp
// tests/FlowHub.Web.ComponentTests/Pipeline/PipelineTestBase.cs
using FlowHub.Core.Captures;
using FlowHub.Core.Classification;
using FlowHub.Core.Skills;
using FlowHub.Web.Stubs;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace FlowHub.Web.ComponentTests.Pipeline;

/// <summary>
/// Builds a MassTransit test harness with a real CaptureServiceStub and substitute
/// classifier / integrations. Tests configure the substitutes per case.
/// </summary>
internal static class PipelineTestBase
{
    public static ServiceProvider Build(
        Action<ServiceCollection>? configure = null,
        Action<IBusRegistrationConfigurator>? configureBus = null)
    {
        var services = new ServiceCollection();

        services.AddSingleton<ICaptureService, CaptureServiceStub>();
        services.AddSingleton(Substitute.For<IClassifier>());
        services.AddSingleton(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(Microsoft.Extensions.Logging.ILogger<>),
            typeof(Microsoft.Extensions.Logging.Abstractions.NullLogger<>));

        configure?.Invoke(services);

        services.AddMassTransitTestHarness(cfg =>
        {
            configureBus?.Invoke(cfg);
        });

        return services.BuildServiceProvider(true);
    }
}
```

- [ ] **Step 8.2: Write failing consumer tests**

```csharp
// tests/FlowHub.Web.ComponentTests/Pipeline/CaptureEnrichmentConsumerTests.cs
using FlowHub.Core.Captures;
using FlowHub.Core.Classification;
using FlowHub.Core.Events;
using FlowHub.Web.Pipeline;
using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace FlowHub.Web.ComponentTests.Pipeline;

public sealed class CaptureEnrichmentConsumerTests
{
    [Fact]
    public async Task Consume_UrlContent_PublishesCaptureClassifiedAndMarksClassified()
    {
        await using var provider = PipelineTestBase.Build(
            configure: s => s.AddSingleton(StubClassifier(new ClassificationResult(["link"], "Wallabag"))),
            configureBus: x => x.AddConsumer<CaptureEnrichmentConsumer>());

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var captureService = provider.GetRequiredService<ICaptureService>();
        var capture = await captureService.SubmitAsync("https://example.com", ChannelKind.Web, default);

        (await harness.Consumed.Any<CaptureCreated>(
            x => x.Context.Message.CaptureId == capture.Id))
            .Should().BeTrue();

        (await harness.Published.Any<CaptureClassified>(
            x => x.Context.Message.CaptureId == capture.Id
                && x.Context.Message.MatchedSkill == "Wallabag"))
            .Should().BeTrue();

        var stored = await captureService.GetByIdAsync(capture.Id, default);
        stored!.Stage.Should().Be(LifecycleStage.Classified);
        stored.MatchedSkill.Should().Be("Wallabag");
    }

    [Fact]
    public async Task Consume_EmptyClassification_MarksOrphanWithoutPublishingClassified()
    {
        await using var provider = PipelineTestBase.Build(
            configure: s => s.AddSingleton(StubClassifier(new ClassificationResult(["unsorted"], string.Empty))),
            configureBus: x => x.AddConsumer<CaptureEnrichmentConsumer>());

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var captureService = provider.GetRequiredService<ICaptureService>();
        var capture = await captureService.SubmitAsync("plain text", ChannelKind.Web, default);

        (await harness.Consumed.Any<CaptureCreated>(
            x => x.Context.Message.CaptureId == capture.Id))
            .Should().BeTrue();

        (await harness.Published.Any<CaptureClassified>())
            .Should().BeFalse();

        var stored = await captureService.GetByIdAsync(capture.Id, default);
        stored!.Stage.Should().Be(LifecycleStage.Orphan);
    }

    private static IClassifier StubClassifier(ClassificationResult result)
    {
        var sub = Substitute.For<IClassifier>();
        sub.ClassifyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(result);
        return sub;
    }
}
```

- [ ] **Step 8.3: Run; expect compile failure (no `CaptureEnrichmentConsumer`)**

Run: `dotnet test tests/FlowHub.Web.ComponentTests --filter FullyQualifiedName~CaptureEnrichmentConsumerTests`
Expected: FAIL — type not found.

- [ ] **Step 8.4: Implement the consumer**

```csharp
// source/FlowHub.Web/Pipeline/CaptureEnrichmentConsumer.cs
using FlowHub.Core.Captures;
using FlowHub.Core.Classification;
using FlowHub.Core.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace FlowHub.Web.Pipeline;

public sealed class CaptureEnrichmentConsumer : IConsumer<CaptureCreated>
{
    private readonly IClassifier _classifier;
    private readonly ICaptureService _captureService;
    private readonly ILogger<CaptureEnrichmentConsumer> _logger;

    public CaptureEnrichmentConsumer(
        IClassifier classifier,
        ICaptureService captureService,
        ILogger<CaptureEnrichmentConsumer> logger)
    {
        _classifier = classifier;
        _captureService = captureService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CaptureCreated> context)
    {
        var msg = context.Message;
        var ct = context.CancellationToken;

        var result = await _classifier.ClassifyAsync(msg.Content, ct);

        if (string.IsNullOrEmpty(result.MatchedSkill))
        {
            await _captureService.MarkOrphanAsync(msg.CaptureId, "no skill matched during classification", ct);
            _logger.LogInformation("Capture {CaptureId} classified as Orphan (no matched skill)", msg.CaptureId);
            return;
        }

        await _captureService.MarkClassifiedAsync(msg.CaptureId, result.MatchedSkill, ct);

        await context.Publish(new CaptureClassified(
            msg.CaptureId,
            result.Tags,
            result.MatchedSkill,
            DateTimeOffset.UtcNow));
    }
}
```

- [ ] **Step 8.5: Run; expect pass**

Run: `dotnet test tests/FlowHub.Web.ComponentTests --filter FullyQualifiedName~CaptureEnrichmentConsumerTests`
Expected: 2 passed.

- [ ] **Step 8.6: Commit**

```bash
git add source/FlowHub.Web/Pipeline/CaptureEnrichmentConsumer.cs tests/FlowHub.Web.ComponentTests/Pipeline/PipelineTestBase.cs tests/FlowHub.Web.ComponentTests/Pipeline/CaptureEnrichmentConsumerTests.cs
git commit -m "feat(pipeline): add CaptureEnrichmentConsumer with Orphan branch"
```

---

## Task 9: TDD `SkillRoutingConsumer`

**Files:**
- Create: `tests/FlowHub.Web.ComponentTests/Pipeline/SkillRoutingConsumerTests.cs`
- Create: `source/FlowHub.Web/Pipeline/SkillRoutingConsumer.cs`

- [ ] **Step 9.1: Write failing tests**

```csharp
// tests/FlowHub.Web.ComponentTests/Pipeline/SkillRoutingConsumerTests.cs
using FlowHub.Core.Captures;
using FlowHub.Core.Events;
using FlowHub.Core.Skills;
using FlowHub.Web.Pipeline;
using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace FlowHub.Web.ComponentTests.Pipeline;

public sealed class SkillRoutingConsumerTests
{
    [Fact]
    public async Task Consume_KnownSkill_CallsIntegrationAndMarksRouted()
    {
        var integration = Substitute.For<ISkillIntegration>();
        integration.Name.Returns("Wallabag");
        integration.WriteAsync(Arg.Any<Capture>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        await using var provider = PipelineTestBase.Build(
            configure: s => s.AddSingleton(integration),
            configureBus: x => x.AddConsumer<SkillRoutingConsumer>());

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var captureService = provider.GetRequiredService<ICaptureService>();
        var capture = await captureService.SubmitAsync("https://example.com", ChannelKind.Web, default);
        await captureService.MarkClassifiedAsync(capture.Id, "Wallabag", default);

        await harness.Bus.Publish(new CaptureClassified(
            capture.Id, ["link"], "Wallabag", DateTimeOffset.UtcNow));

        (await harness.Consumed.Any<CaptureClassified>(
            x => x.Context.Message.CaptureId == capture.Id))
            .Should().BeTrue();

        await integration.Received(1).WriteAsync(
            Arg.Is<Capture>(c => c.Id == capture.Id),
            Arg.Any<IReadOnlyList<string>>(),
            Arg.Any<CancellationToken>());

        (await captureService.GetByIdAsync(capture.Id, default))!.Stage.Should().Be(LifecycleStage.Routed);
    }

    [Fact]
    public async Task Consume_UnknownSkill_MarksUnhandled()
    {
        var integration = Substitute.For<ISkillIntegration>();
        integration.Name.Returns("Wallabag");

        await using var provider = PipelineTestBase.Build(
            configure: s => s.AddSingleton(integration),
            configureBus: x => x.AddConsumer<SkillRoutingConsumer>());

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var captureService = provider.GetRequiredService<ICaptureService>();
        var capture = await captureService.SubmitAsync("hello", ChannelKind.Web, default);
        await captureService.MarkClassifiedAsync(capture.Id, "DoesNotExist", default);

        await harness.Bus.Publish(new CaptureClassified(
            capture.Id, ["unknown"], "DoesNotExist", DateTimeOffset.UtcNow));

        (await harness.Consumed.Any<CaptureClassified>()).Should().BeTrue();

        await integration.DidNotReceive().WriteAsync(
            Arg.Any<Capture>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>());

        (await captureService.GetByIdAsync(capture.Id, default))!.Stage.Should().Be(LifecycleStage.Unhandled);
    }
}
```

- [ ] **Step 9.2: Run; expect compile failure**

Run: `dotnet test tests/FlowHub.Web.ComponentTests --filter FullyQualifiedName~SkillRoutingConsumerTests`
Expected: FAIL — `SkillRoutingConsumer` not found.

- [ ] **Step 9.3: Implement the consumer**

```csharp
// source/FlowHub.Web/Pipeline/SkillRoutingConsumer.cs
using FlowHub.Core.Captures;
using FlowHub.Core.Events;
using FlowHub.Core.Skills;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace FlowHub.Web.Pipeline;

public sealed class SkillRoutingConsumer : IConsumer<CaptureClassified>
{
    private readonly IEnumerable<ISkillIntegration> _integrations;
    private readonly ICaptureService _captureService;
    private readonly ILogger<SkillRoutingConsumer> _logger;

    public SkillRoutingConsumer(
        IEnumerable<ISkillIntegration> integrations,
        ICaptureService captureService,
        ILogger<SkillRoutingConsumer> logger)
    {
        _integrations = integrations;
        _captureService = captureService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CaptureClassified> context)
    {
        var msg = context.Message;
        var ct = context.CancellationToken;

        var integration = _integrations.FirstOrDefault(i =>
            string.Equals(i.Name, msg.MatchedSkill, StringComparison.Ordinal));

        if (integration is null)
        {
            await _captureService.MarkUnhandledAsync(msg.CaptureId,
                $"no integration registered for skill '{msg.MatchedSkill}'", ct);
            _logger.LogInformation(
                "Capture {CaptureId} marked Unhandled — no integration for skill {Skill}",
                msg.CaptureId, msg.MatchedSkill);
            return;
        }

        var capture = await _captureService.GetByIdAsync(msg.CaptureId, ct)
            ?? throw new InvalidOperationException($"Capture {msg.CaptureId} not found in store.");

        await integration.WriteAsync(capture, msg.Tags, ct);
        await _captureService.MarkRoutedAsync(msg.CaptureId, ct);
    }
}
```

- [ ] **Step 9.4: Run; expect pass**

Run: `dotnet test tests/FlowHub.Web.ComponentTests --filter FullyQualifiedName~SkillRoutingConsumerTests`
Expected: 2 passed.

- [ ] **Step 9.5: Commit**

```bash
git add source/FlowHub.Web/Pipeline/SkillRoutingConsumer.cs tests/FlowHub.Web.ComponentTests/Pipeline/SkillRoutingConsumerTests.cs
git commit -m "feat(pipeline): add SkillRoutingConsumer with Unknown-skill→Unhandled branch"
```

---

## Task 10: TDD `LifecycleFaultObserver`

**Files:**
- Create: `tests/FlowHub.Web.ComponentTests/Pipeline/LifecycleFaultObserverTests.cs`
- Create: `source/FlowHub.Web/Pipeline/LifecycleFaultObserver.cs`

- [ ] **Step 10.1: Write failing tests**

```csharp
// tests/FlowHub.Web.ComponentTests/Pipeline/LifecycleFaultObserverTests.cs
using FlowHub.Core.Captures;
using FlowHub.Core.Classification;
using FlowHub.Core.Events;
using FlowHub.Core.Skills;
using FlowHub.Web.Pipeline;
using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace FlowHub.Web.ComponentTests.Pipeline;

public sealed class LifecycleFaultObserverTests
{
    [Fact]
    public async Task EnrichmentExhaustedRetries_MarksOrphan()
    {
        var classifier = Substitute.For<IClassifier>();
        classifier.ClassifyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<ClassificationResult>(_ => throw new InvalidOperationException("boom"));

        await using var provider = PipelineTestBase.Build(
            configure: s => s.AddSingleton(classifier),
            configureBus: x =>
            {
                x.AddConsumer<CaptureEnrichmentConsumer>(c =>
                    c.UseMessageRetry(r => r.Intervals(10, 10)));
                x.AddConsumer<LifecycleFaultObserver>();
            });

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var captureService = provider.GetRequiredService<ICaptureService>();
        var capture = await captureService.SubmitAsync("hello", ChannelKind.Web, default);

        (await harness.Consumed.Any<Fault<CaptureCreated>>(
            x => x.Context.Message.Message.CaptureId == capture.Id,
            TimeSpan.FromSeconds(5)))
            .Should().BeTrue();

        (await captureService.GetByIdAsync(capture.Id, default))!.Stage.Should().Be(LifecycleStage.Orphan);
    }

    [Fact]
    public async Task RoutingExhaustedRetries_MarksUnhandled()
    {
        var integration = Substitute.For<ISkillIntegration>();
        integration.Name.Returns("Wallabag");
        integration.WriteAsync(Arg.Any<Capture>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("integration down"));

        await using var provider = PipelineTestBase.Build(
            configure: s => s.AddSingleton(integration),
            configureBus: x =>
            {
                x.AddConsumer<SkillRoutingConsumer>(c =>
                    c.UseMessageRetry(r => r.Intervals(10, 10, 10)));
                x.AddConsumer<LifecycleFaultObserver>();
            });

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var captureService = provider.GetRequiredService<ICaptureService>();
        var capture = await captureService.SubmitAsync("https://example.com", ChannelKind.Web, default);
        await captureService.MarkClassifiedAsync(capture.Id, "Wallabag", default);

        await harness.Bus.Publish(new CaptureClassified(
            capture.Id, ["link"], "Wallabag", DateTimeOffset.UtcNow));

        (await harness.Consumed.Any<Fault<CaptureClassified>>(
            x => x.Context.Message.Message.CaptureId == capture.Id,
            TimeSpan.FromSeconds(5)))
            .Should().BeTrue();

        (await captureService.GetByIdAsync(capture.Id, default))!.Stage.Should().Be(LifecycleStage.Unhandled);
    }
}
```

- [ ] **Step 10.2: Run; expect compile failure**

Run: `dotnet test tests/FlowHub.Web.ComponentTests --filter FullyQualifiedName~LifecycleFaultObserverTests`
Expected: FAIL — `LifecycleFaultObserver` not found.

- [ ] **Step 10.3: Implement the observer**

```csharp
// source/FlowHub.Web/Pipeline/LifecycleFaultObserver.cs
using FlowHub.Core.Captures;
using FlowHub.Core.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace FlowHub.Web.Pipeline;

public sealed class LifecycleFaultObserver
    : IConsumer<Fault<CaptureCreated>>, IConsumer<Fault<CaptureClassified>>
{
    private readonly ICaptureService _captureService;
    private readonly ILogger<LifecycleFaultObserver> _logger;

    public LifecycleFaultObserver(
        ICaptureService captureService,
        ILogger<LifecycleFaultObserver> logger)
    {
        _captureService = captureService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<Fault<CaptureCreated>> context)
    {
        var captureId = context.Message.Message.CaptureId;
        var reason = FormatReason(context.Message);

        try
        {
            await _captureService.MarkOrphanAsync(captureId, reason, context.CancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "LifecycleFaultObserver failed to mark capture {CaptureId} as Orphan", captureId);
        }
    }

    public async Task Consume(ConsumeContext<Fault<CaptureClassified>> context)
    {
        var captureId = context.Message.Message.CaptureId;
        var reason = FormatReason(context.Message);

        try
        {
            await _captureService.MarkUnhandledAsync(captureId, reason, context.CancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "LifecycleFaultObserver failed to mark capture {CaptureId} as Unhandled", captureId);
        }
    }

    private static string FormatReason<T>(Fault<T> fault) where T : class
    {
        var first = fault.Exceptions?.FirstOrDefault();
        return first is null
            ? "exhausted retries: <no exception detail>"
            : $"exhausted retries: {first.ExceptionType}: {first.Message}";
    }
}
```

- [ ] **Step 10.4: Run; expect pass**

Run: `dotnet test tests/FlowHub.Web.ComponentTests --filter FullyQualifiedName~LifecycleFaultObserverTests`
Expected: 2 passed.

- [ ] **Step 10.5: Commit**

```bash
git add source/FlowHub.Web/Pipeline/LifecycleFaultObserver.cs tests/FlowHub.Web.ComponentTests/Pipeline/LifecycleFaultObserverTests.cs
git commit -m "feat(pipeline): add LifecycleFaultObserver mapping Fault<T> to Orphan/Unhandled"
```

---

## Task 11: Wire MassTransit + classifier + integrations in `Program.cs`

**Files:**
- Modify: `source/FlowHub.Web/Program.cs`
- Modify: `source/FlowHub.Web/appsettings.Development.json`

- [ ] **Step 11.1: Add registrations**

In `source/FlowHub.Web/Program.cs`, replace the Block 2 stub section (the three `AddSingleton` lines plus surrounding comments) with:

```csharp
// Block 2 stub services — replaced incrementally as later blocks land.
builder.Services.AddSingleton<ICaptureService, CaptureServiceStub>();
builder.Services.AddSingleton<ISkillRegistry, SkillRegistryStub>();
builder.Services.AddSingleton<IIntegrationHealthService, IntegrationHealthServiceStub>();

// Block 3 Slice B — classifier + skill integrations.
builder.Services.AddSingleton<IClassifier, KeywordClassifier>();
builder.Services.AddSingleton<ISkillIntegration>(sp =>
    new LoggingSkillIntegration("Wallabag", sp.GetRequiredService<ILogger<LoggingSkillIntegration>>()));
builder.Services.AddSingleton<ISkillIntegration>(sp =>
    new LoggingSkillIntegration("Vikunja", sp.GetRequiredService<ILogger<LoggingSkillIntegration>>()));

// Block 3 Slice B — MassTransit pipeline.
builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();

    x.AddConsumer<CaptureEnrichmentConsumer>(c =>
        c.UseMessageRetry(r => r.Intervals(100, 500)));

    x.AddConsumer<SkillRoutingConsumer>(c =>
        c.UseMessageRetry(r => r.Intervals(500, 2000, 5000)));

    x.AddConsumer<LifecycleFaultObserver>();

    if (string.Equals(builder.Configuration["Bus:Transport"], "RabbitMq", StringComparison.OrdinalIgnoreCase))
    {
        x.UsingRabbitMq((ctx, cfg) =>
        {
            cfg.Host(builder.Configuration["Bus:RabbitMq:Host"]);
            cfg.ConfigureEndpoints(ctx);
        });
    }
    else
    {
        x.UsingInMemory((ctx, cfg) => cfg.ConfigureEndpoints(ctx));
    }
});
```

Then update the `using` block at the top to include:

```csharp
using FlowHub.Core.Classification;
using FlowHub.Core.Skills;
using FlowHub.Web.Pipeline;
using MassTransit;
using Microsoft.Extensions.Logging;
```

- [ ] **Step 11.2: Default the bus transport in Development**

In `source/FlowHub.Web/appsettings.Development.json`, add or merge a `Bus` section:

```json
{
  "Bus": {
    "Transport": "InMemory"
  }
}
```

(Keep any existing top-level keys intact.)

- [ ] **Step 11.3: Build**

Run: `make build`
Expected: build succeeds.

- [ ] **Step 11.4: Smoke-run the app**

Run: `make run` in one shell. In a second shell:

```bash
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5070/
# Expected: 200
```

Stop the dev server (Ctrl+C in the first shell). The app should boot without bus errors.

- [ ] **Step 11.5: Commit**

```bash
git add source/FlowHub.Web/Program.cs source/FlowHub.Web/appsettings.Development.json
git commit -m "feat(web): wire MassTransit pipeline + classifier + skill integrations"
```

---

## Task 12: Full test-suite pass

- [ ] **Step 12.1: Run the entire suite**

Run: `dotnet test FlowHub.slnx`
Expected: all tests pass — at minimum the new Slice-B tests (~15+ cases) plus the existing component tests.

- [ ] **Step 12.2: If any pre-existing test broke**, fix the test (do not modify production code to mask a real regression). Document the fix in the commit message.

- [ ] **Step 12.3: Commit only if fixes were needed**, otherwise skip.

---

## Task 13: Author ADR 0003

**Files:**
- Create: `docs/adr/0003-async-pipeline.md`

- [ ] **Step 13.1: Write the ADR**

Use ADR 0002 as the structural template — same headings (Status, Context, Decision, Consequences, Alternatives Considered, Consequences for the next blocks, References). Distil from the spec at `docs/superpowers/specs/2026-04-30-async-pipeline-design.md`. The ADR should be the durable decision record (≈500–800 lines, not the full design narrative). Include sections for each of D1–D13 from the spec's Decisions table — roll them into prose under the appropriate ADR headings.

Required content checklist for the ADR:

- Status: Accepted, Date: 2026-04-30
- Context: ADR 0002's deferred questions; the Block 3 Auftrag's explicit queue requirement
- Decision: the eight numbered decisions from ADR 0002 expanded — async surface, event vocabulary, classifier port, retry policy, fault observer, Orphan/Unhandled mapping, endpoint formatter, deployment topology
- Alternatives considered: per-consumer vs global retry, observer vs `_error`-only DLQ, Outbox now vs deferred
- Consequences: rubric coverage, complexity tax, in-memory-transport caveats
- References: the brainstorming spec, the api-surface sketch, ADR 0002, the implementation plan

- [ ] **Step 13.2: Commit**

```bash
git add docs/adr/0003-async-pipeline.md
git commit -m "docs(adr): accept ADR 0003 — async pipeline (MassTransit topology + retry/fault)"
```

---

## Task 14: Bootstrap `docs/ai-usage.md`

**Files:**
- Create: `docs/ai-usage.md`

The 12-pt KI-Werkzeug-Nutzung rubric criterion needs evidence accumulating from the start of the block.

- [ ] **Step 14.1: Write the initial document**

Use the following structure:

```markdown
# AI Tool Usage — FlowHub

> Living document for the rubric criterion "Wurden KI-unterstützende Werkzeuge verwendet und deren Nutzung beschrieben (12)" (Block-3 Bewertungskriterien). Updated continuously as work happens — not at submission time.

## Tools in use

| Tool | Purpose | Where it shows up |
|---|---|---|
| Claude Code (Opus 4.7) | Code generation, brainstorming, plan writing, ADR drafting | ~all FlowHub source under `source/`, all docs under `docs/superpowers/`, ADRs |
| GitHub Copilot | Inline suggestions during editing | Mostly skipped — Claude Code drives sessions end-to-end |
| ChatGPT | Ad-hoc concept clarification, side checks | Only when Claude is mid-task on something else |

## Block 3 Slice B — async pipeline

**Brainstorming + spec writing** (`docs/superpowers/specs/2026-04-30-async-pipeline-design.md`):
- Conversational design via the Claude Code `superpowers:brainstorming` skill, ~13 decisions surfaced as A/B/C trade-off questions.
- Reviewer (the human) corrected scope choice from "Slice A first" to "Slice B (ADR 0003 first)" after the technical review of MassTransit's tradeoffs.
- Final spec self-reviewed by the AI before commit (placeholder scan, internal consistency, ambiguity check).

**Implementation plan** (`docs/superpowers/plans/2026-04-30-async-pipeline.md`):
- Authored by Claude Code via the `superpowers:writing-plans` skill from the approved spec.
- Bite-sized TDD tasks; engineer (or executing AI) writes failing test → implementation → green → commit per step.

**Generated vs. handwritten share (estimate, slice B only):**
- Spec + plan: ~95% AI-drafted, ~5% human-edited (decisions, scope, factual corrections)
- Code (consumers, classifier, registration): TBC during execution — record actual after each task completes.

## Reflexion — what worked, what didn't

(Filled in continuously as the block progresses; final reflection lands in the Block-3 PVA presentation material.)

- ✅ ...
- ⚠ ...
- ❌ ...

## Prompts of note

(Capture surprising or high-leverage prompts here. Empty for now.)
```

- [ ] **Step 14.2: Commit**

```bash
git add docs/ai-usage.md
git commit -m "docs(ai-usage): bootstrap living doc for KI-Werkzeug-Nutzung rubric"
```

---

## Task 15: `docker-compose.yml` Block-5 sketch

**Files:**
- Create: `docker-compose.yml`

This is a sketch — not invoked by `make run` and not required to build.

- [ ] **Step 15.1: Write the sketch**

```yaml
# docker-compose.yml
# Block-5 deployment topology preview. NOT used by `make run` (which stays single-process).
# Demonstrates the multi-container option: FlowHub.Web + FlowHub.Api (Slice A) + RabbitMQ.
# Run with: docker compose up --build (once the Slice-A api project lands and Dockerfiles exist).

services:
  flowhub.web:
    build:
      context: .
      dockerfile: source/FlowHub.Web/Dockerfile
    image: flowhub-web:dev
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ASPNETCORE_URLS: http://+:8080
      Bus__Transport: RabbitMq
      Bus__RabbitMq__Host: rabbitmq
    depends_on:
      - rabbitmq
    ports:
      - "5070:8080"

  # flowhub.api lands in Slice A — entry kept here so the topology is visible.
  # flowhub.api:
  #   build:
  #     context: .
  #     dockerfile: source/FlowHub.Api/Dockerfile
  #   image: flowhub-api:dev
  #   environment:
  #     Bus__Transport: RabbitMq
  #     Bus__RabbitMq__Host: rabbitmq
  #   depends_on: [rabbitmq]

  rabbitmq:
    image: rabbitmq:3-management-alpine
    ports:
      - "15672:15672"   # management UI
      - "5672:5672"     # AMQP
```

- [ ] **Step 15.2: Commit**

```bash
git add docker-compose.yml
git commit -m "build(deploy): add docker-compose.yml sketch for Block-5 topology"
```

---

## Task 16: Update Block-3 Nachbereitung checklist + push

**Files:**
- Modify: `vault/Blöcke/03 Service/03 Service - c) Nachbereitung.md`

- [ ] **Step 16.1: Tick off completed Slice-B items**

In `vault/Blöcke/03 Service/03 Service - c) Nachbereitung.md`, change the following bullets from `- [ ]` to `- [x]`:

- ADR 0003 — Async Messaging Pipeline
- MassTransit + In-Memory-Transport in `FlowHub.Web` registriert
- Event-Kontrakte definiert (just `CaptureCreated`, `CaptureClassified` — note the dropped events in a parenthetical)
- Consumers: `CaptureEnrichmentConsumer`, `SkillRoutingHandler` (note actual class name `SkillRoutingConsumer`)
- Retry-/DLQ-Policy + Test Harness Tests
- RabbitMQ-Profil vorbereiten (Docker-Compose-Snippet, lokal aktivierbar)

Update the page's `updated:` frontmatter to `2026-04-30`.

- [ ] **Step 16.2: Commit**

```bash
git add vault/Blöcke/
git commit -m "docs(vault/blöcke): tick off Slice-B items in Block 3 Nachbereitung"
```

- [ ] **Step 16.3: Push (ASK USER FIRST)**

Do not push without explicit user confirmation. Once confirmed:

```bash
git push origin main
```

---

## Self-review notes

Spec coverage check:

- D1 async surface (flows 1+2) → Tasks 7, 8, 9
- D2 events (`CaptureCreated`, `CaptureClassified`) → Task 2
- D3 `IClassifier` + `KeywordClassifier` → Tasks 3, 4
- D4 per-consumer retry → Task 11 (`Intervals(100, 500)`, `Intervals(500, 2000, 5000)`)
- D5 `LifecycleFaultObserver` → Task 10
- D6 empty classification → `Orphan` directly → Task 8 step 8.4
- D7 `LoggingSkillIntegration` → Task 5
- D8/D9 outbox + idempotency deferred → no task; documented in ADR 0003 (Task 13) and `docs/ai-usage.md`
- D10 `KebabCaseEndpointNameFormatter` → Task 11
- D11 multi-container Block-5 sketch → Task 15
- D12 tests in `FlowHub.Web.ComponentTests/Pipeline/` → Tasks 8–10
- D13 `ai-usage.md` bootstrap → Task 14

All 13 decisions covered. No placeholders in code blocks; one `<MT_VERSION>` placeholder in Task 1 is intentional and explained.

Type consistency check:

- `CaptureCreated.CaptureId` and `CaptureClassified.CaptureId` — same name, same type (`Guid`) ✓
- `IClassifier.ClassifyAsync` returns `Task<ClassificationResult>` everywhere ✓
- `ISkillIntegration.WriteAsync(Capture, IReadOnlyList<string>, CancellationToken)` consistent across consumer + tests ✓
- `ICaptureService.MarkClassifiedAsync(Guid, string, CancellationToken)` — note: takes `string matchedSkill`, not `ClassificationResult`. The spec's earlier draft mentioned `ClassificationResult`, but Tags are not persisted on `Capture` (per spec), so the simpler `string` is right. Plan and code agree.

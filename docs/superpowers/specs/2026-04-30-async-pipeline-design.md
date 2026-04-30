---
title: Async Pipeline (Slice B) — Brainstorming Spec
status: Draft (awaiting user review)
date: 2026-04-30
block: 3 — Service · Nachbereitung
related:
  - docs/adr/0001-frontend-render-mode-and-architecture.md
  - docs/adr/0002-service-architecture-and-async-communication.md
  - docs/design/api/api-surface.md
  - vault/Blöcke/03 Service/03 Service - c) Nachbereitung.md
---

# Async Pipeline (Slice B) — Brainstorming Spec

## Context

ADR 0002 picked **MassTransit** (in-memory transport in dev/test, RabbitMQ in prod from Block 5) and named `CaptureCreated`, `CaptureClassified`, and `SkillRouted` as candidate events. It deliberately deferred topology, retry/DLQ specifics, fault-handling, and the exact set of flows that go through the bus to a follow-up ADR. This spec resolves those open questions and is the brainstorming artifact for **Slice B of Block 3 Nachbereitung**: ADR 0003 + the working MassTransit pipeline + first consumers + tests.

The Block 3 Auftrag explicitly names *"asynchrone Kommunikation mittels einer Queue"*; the rubric's *KI / Sub-Systeme / Reflexion* bucket additionally awards 5 pts for *"Sub-Systeme als unabhängige Container deploybar"*. Both are addressed without splitting the codebase: in code FlowHub stays a Modular Monolith per ADR 0002, but a `docker-compose.yml` sketch demonstrates the multi-container topology for Block 5.

## Scope

**In (Slice B deliverables):**

- Two event contracts in `FlowHub.Core/Events/`: `CaptureCreated`, `CaptureClassified`
- `IClassifier` port + `KeywordClassifier` stub adapter in `FlowHub.Core/Classification/`
- `ISkillIntegration` port + `LoggingSkillIntegration` stub adapter
- Two consumers + one fault observer in `FlowHub.Web/Pipeline/`
- MassTransit registration in `FlowHub.Web/Program.cs` (in-memory transport, kebab-case endpoint formatter)
- Per-consumer retry policy
- `LifecycleFaultObserver` mapping `Fault<T>` → `LifecycleStage`
- MassTransit Test Harness tests in `tests/FlowHub.Web.ComponentTests/Pipeline/`
- `docker-compose.yml` sketch (multi-container preview, not the runtime default)
- ADR 0003 authored from this spec during execution
- `docs/ai-usage.md` bootstrapped as a side-effect (rubric leverage)

**Out (deferred):**

- Real AI classifier (Slice C / ADR 0004)
- Real persistence and Outbox pattern (Block 4)
- Real integration adapters — Wallabag, Wekan, Vikunja (Block 4/5)
- RabbitMQ deployment (Block 5)
- Sagas / state machines (rejected per ADR 0002)
- `IRequestClient` / RPC over the bus (rejected per ADR 0002)
- Multi-process runtime (one deployment unit per ADR 0002:158)

## Decisions (from brainstorming)

| # | Decision | Rationale |
|---|---|---|
| **D1** | Async surface = flows 1 + 2 only. `Submit` → enrichment → routing-with-inline-integration-write. | Decouples slow work from the request path (Telegram bot, REST submit) and keeps the integration-failure DLQ story. Flow 3 (separate routing event) adds an unnecessary hop; flow 4 (manual retry) is just a synchronous endpoint that re-publishes `CaptureCreated`. |
| **D2** | Event vocabulary = `CaptureCreated`, `CaptureClassified`. | Two flows = two events. `SkillRoutingRequested`, `IntegrationCallFailed`, and `SkillRouted` from earlier drafts are dropped — routing happens inline; failures surface as `Fault<CaptureClassified>`. |
| **D3** | Classification = `IClassifier` port + `KeywordClassifier` stub. | Slice C swaps in `AiClassifier : IClassifier` without touching consumer code. Hexagonal port shape earns *"Code lesbar / nach Layer strukturiert"* rubric points. |
| **D4** | Retry policy is per-consumer. Enrichment: `Intervals(100, 500)`. Routing: `Intervals(500, 2000, 5000)`. | Reflects the actual cost/latency profile of each consumer (in-process classification vs cross-process integration). Gives ADR 0003 something concrete to argue rather than handwaving. |
| **D5** | `LifecycleFaultObserver` updates capture state on `Fault<T>`. `Fault<CaptureCreated>` → `Orphan`; `Fault<CaptureClassified>` → `Unhandled`. | Without it, the queue's resilience story is invisible — captures rot in `_error` while their `LifecycleStage` stays at `Raw`/`Classified`. The Dashboard FailureCounts tile and the API's `?stage=` filter both need these states actually being set. |
| **D6** | Empty classification result → enrichment consumer sets `LifecycleStage=Orphan` **directly** (no fault, no `CaptureClassified` published). | Cleaner state machine: faults fire only on unexpected exceptions, not on successful-but-empty classifications. Gives `Orphan` two explicit entry points (no-skill-match, exhausted-retry) instead of bundling them via the fault path. |
| **D7** | Routing integration in Slice B = `LoggingSkillIntegration` stub returning success. | Real Wallabag/Wekan/Vikunja adapters are Block 4/5 work. The stub keeps Slice B end-to-end testable without committing to integration code. |
| **D8** | Outbox pattern deferred to Block 4. | No persistence yet → no outbox surface. Note in ADR 0003 as future work. |
| **D9** | Idempotency: best-effort in Slice B; formal `IdempotencyReceiver` filter deferred to Block 4. | In-memory transport is single-process and won't redeliver. RabbitMQ at-least-once warrants idempotency, but persistence is the prerequisite. Consumers SHOULD lookup by `CaptureId` before mutating state. |
| **D10** | Endpoint name formatter = `KebabCaseEndpointNameFormatter`. | Stable queue names across in-memory and RabbitMQ transports (`capture-enrichment` rather than `CaptureEnrichmentConsumer`). |
| **D11** | Co-host stays — one process in code, multi-container in Block-5 deployment plan. | Resolves rubric tension with ADR 0002:158: keep the Modular Monolith (`make run` boots everything), but ship a `docker-compose.yml` sketch demonstrating API + Web + RabbitMQ as separable containers when Block 5 lands. |
| **D12** | Tests live in `tests/FlowHub.Web.ComponentTests/Pipeline/` for now. | YAGNI on a separate test project; bus is registered in `FlowHub.Web/Program.cs` so the existing project is the closest fit. Split later if the suite grows. |
| **D13** | `docs/ai-usage.md` is bootstrapped during this slice as a side-effect, not as a separate slice. | The 12-pt KI-Werkzeug-Nutzung criterion needs evidence accumulating from day one. We log AI tools used to produce this spec and the implementation. |

## Architecture

```
                   ┌──────────────────────────────────────────────────────────┐
                   │  FlowHub.Web (single process, Modular Monolith)          │
                   │                                                          │
   POST /captures──┼─►ICaptureService.Submit ──┐                              │
   (REST or                                    │ Publish(CaptureCreated)      │
    Telegram)      │                           ▼                              │
                   │                    ╔═══════════════════════╗             │
                   │                    ║  MassTransit bus      ║             │
                   │                    ║  (in-memory in dev)   ║             │
                   │                    ╚═══════════════════════╝             │
                   │                           │                              │
                   │   CaptureCreated ◄────────┤                              │
                   │           │               │                              │
                   │           ▼               │                              │
                   │  ┌────────────────────┐   │                              │
                   │  │ CaptureEnrichment  │   │                              │
                   │  │     Consumer       │   │                              │
                   │  │ (uses IClassifier) │   │                              │
                   │  └─────┬─────────┬────┘   │                              │
                   │        │         │        │                              │
                   │  no skill?      ok?       │                              │
                   │        │         │        │                              │
                   │        ▼         │        │                              │
                   │   stage=Orphan   │        │                              │
                   │                  ▼        │                              │
                   │           Publish(CaptureClassified)                     │
                   │                  │        │                              │
                   │  CaptureClassified ◄──────┤                              │
                   │                  │        │                              │
                   │                  ▼        │                              │
                   │  ┌────────────────────┐   │                              │
                   │  │  SkillRouting      │   │                              │
                   │  │     Consumer       │   │                              │
                   │  │ (uses IIntegration)│   │                              │
                   │  └─────────┬──────────┘   │                              │
                   │            │              │                              │
                   │            ▼              │                              │
                   │     stage=Routed          │                              │
                   │                                                          │
                   │  After exhausted retries:                                │
                   │  Fault<CaptureCreated>    ─► LifecycleFaultObserver      │
                   │  Fault<CaptureClassified> ─► LifecycleFaultObserver      │
                   │  (sets stage = Orphan / Unhandled)                       │
                   └──────────────────────────────────────────────────────────┘
```

## Components

### Event contracts — `FlowHub.Core/Events/`

```csharp
namespace FlowHub.Core.Events;

public sealed record CaptureCreated(
    Guid CaptureId,
    string Content,
    ChannelKind Source,
    DateTimeOffset CreatedAt);

public sealed record CaptureClassified(
    Guid CaptureId,
    IReadOnlyList<string> Tags,
    string MatchedSkill,
    DateTimeOffset ClassifiedAt);
```

Records, immutable, no methods. `ChannelKind` and other enums serialise as strings via `JsonStringEnumConverter` (matches the API surface convention from `docs/design/api/api-surface.md`).

### Classifier port — `FlowHub.Core/Classification/`

```csharp
public interface IClassifier
{
    Task<ClassificationResult> ClassifyAsync(string content, CancellationToken ct);
}

public sealed record ClassificationResult(
    IReadOnlyList<string> Tags,
    string MatchedSkill);
```

`KeywordClassifier` (Slice B implementation, ~30 LoC):

- Content matches URL pattern → `Tags=["link"]`, `MatchedSkill="Wallabag"`
- Content contains `"todo"` or `"task"` (case-insensitive) → `Tags=["task"]`, `MatchedSkill="Vikunja"`
- Otherwise → `Tags=["unsorted"]`, `MatchedSkill=""` (handled per D6)

### Skill integration port — `FlowHub.Core/Skills/`

```csharp
public interface ISkillIntegration
{
    string Name { get; }
    Task WriteAsync(Capture capture, IReadOnlyList<string> tags, CancellationToken ct);
}
```

`LoggingSkillIntegration(string name)` (Slice B implementation): exposes `Name` from the constructor argument; `WriteAsync` logs `"would write to {Name} for capture {CaptureId}"` and returns successfully.

The routing consumer resolves the matching integration via `IEnumerable<ISkillIntegration>` and selects by `Name`. If no match is found the consumer marks the capture as `Unhandled` directly (D6) — `LoggingSkillIntegration` itself never throws.

### Consumers — `FlowHub.Web/Pipeline/`

**`CaptureEnrichmentConsumer : IConsumer<CaptureCreated>`**

```
Consume(ConsumeContext<CaptureCreated> ctx):
  result = await classifier.ClassifyAsync(ctx.Message.Content, ctx.CancellationToken)
  if string.IsNullOrEmpty(result.MatchedSkill):
      await captureService.MarkOrphanAsync(ctx.Message.CaptureId, ctx.CancellationToken)
      log.Info("capture {Id} classified as Orphan (no matched skill)")
      return
  await captureService.MarkClassifiedAsync(ctx.Message.CaptureId, result, ctx.CancellationToken)
  await ctx.Publish(new CaptureClassified(...))
```

**`SkillRoutingConsumer : IConsumer<CaptureClassified>`**

```
Consume(ConsumeContext<CaptureClassified> ctx):
  integration = integrations.SingleOrDefault(i => i.Name == ctx.Message.MatchedSkill)
  if integration is null:
      await captureService.MarkUnhandledAsync(ctx.Message.CaptureId, "no integration registered", ct)
      return
  capture = await captureService.GetAsync(ctx.Message.CaptureId, ct)
  await integration.WriteAsync(capture, ctx.Message.Tags, ct)
  await captureService.MarkRoutedAsync(ctx.Message.CaptureId, ct)
```

**`LifecycleFaultObserver : IConsumer<Fault<CaptureCreated>>, IConsumer<Fault<CaptureClassified>>`**

One class implementing both fault interfaces.

- `Fault<CaptureCreated>` → `MarkOrphanAsync(captureId, "exhausted retries: {exception}")`
- `Fault<CaptureClassified>` → `MarkUnhandledAsync(captureId, "exhausted retries: {exception}")`
- Logs at `LogLevel.Error` with `CaptureId`, the originating message id, and the `ExceptionInfo` from `Fault.Exceptions`
- Best-effort: if the observer itself throws, log once and let the message rot in the bus's secondary error queue. No recursive retry.

### `ICaptureService` extensions (Slice B additions)

The existing service from Block 2 grows four state-transition methods:

```csharp
Task MarkClassifiedAsync(Guid id, ClassificationResult result, CancellationToken ct);
Task MarkRoutedAsync(Guid id, CancellationToken ct);
Task MarkOrphanAsync(Guid id, string reason, CancellationToken ct);
Task MarkUnhandledAsync(Guid id, string reason, CancellationToken ct);
```

Storage stays in-memory (Block 2 stub backing) — Block 4 swaps in EF Core.

### MassTransit registration — `FlowHub.Web/Program.cs`

```csharp
builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();

    x.AddConsumer<CaptureEnrichmentConsumer>(c =>
        c.UseMessageRetry(r => r.Intervals(100, 500)));

    x.AddConsumer<SkillRoutingConsumer>(c =>
        c.UseMessageRetry(r => r.Intervals(500, 2000, 5000)));

    x.AddConsumer<LifecycleFaultObserver>();

    if (builder.Configuration["Bus:Transport"] == "RabbitMq")
        x.UsingRabbitMq((ctx, cfg) =>
        {
            cfg.Host(builder.Configuration["Bus:RabbitMq:Host"]);
            cfg.ConfigureEndpoints(ctx);
        });
    else
        x.UsingInMemory((ctx, cfg) => cfg.ConfigureEndpoints(ctx));
});

builder.Services.AddSingleton<IClassifier, KeywordClassifier>();

// One stub integration per skill name the KeywordClassifier can produce.
// Real adapters (Wallabag, Vikunja, …) replace these in Block 4/5.
builder.Services.AddSingleton<ISkillIntegration>(_ => new LoggingSkillIntegration("Wallabag"));
builder.Services.AddSingleton<ISkillIntegration>(_ => new LoggingSkillIntegration("Vikunja"));
```

Routing consumer resolves via `IEnumerable<ISkillIntegration>` and selects by `Name`.

`appsettings.Development.json` defaults `Bus:Transport=InMemory`. `appsettings.json` has no `Bus:` section (overridden by env var in production).

## Data flow

### Happy path

1. Telegram bot or REST API calls `ICaptureService.SubmitAsync(content, source, ct)`.
2. Service stores capture in-memory with `LifecycleStage=Raw`.
3. Service publishes `CaptureCreated`.
4. Caller returns to user (Telegram replies "got it"; REST returns `202 Accepted` with `Location` per api-surface.md).
5. `CaptureEnrichmentConsumer` consumes `CaptureCreated`, calls `IClassifier.ClassifyAsync`, sets stage to `Classified`, publishes `CaptureClassified`.
6. `SkillRoutingConsumer` consumes `CaptureClassified`, resolves the matching `ISkillIntegration`, calls `WriteAsync`, sets stage to `Routed`.

### Failure paths

| Trigger | Behaviour | Final state |
|---|---|---|
| Classifier returns empty `MatchedSkill` | Enrichment consumer marks capture directly (D6) | `Orphan` |
| No `ISkillIntegration` registered for matched skill | Routing consumer marks capture directly (D6) | `Unhandled` |
| Enrichment consumer throws past retry budget (`100, 500`) | `Fault<CaptureCreated>` → `LifecycleFaultObserver` | `Orphan` |
| Routing consumer throws past retry budget (`500, 2000, 5000`) | `Fault<CaptureClassified>` → `LifecycleFaultObserver` | `Unhandled` |
| `LifecycleFaultObserver` itself throws | Logged; message rots in secondary error queue | `LifecycleStage` unchanged — operator must reconcile manually |
| Manual retry (`POST /captures/{id}/retry`) | Synchronous endpoint re-publishes `CaptureCreated` for the same id | Stage flips when consumer next succeeds; no automatic rollback to `Raw` |

### LifecycleStage transitions (state machine)

```
              ┌──────────┐
   Submit ──► │   Raw    │
              └────┬─────┘
                   │ enrichment success
                   │ (skill matched)
                   ▼
              ┌──────────────┐
              │  Classified  │
              └────┬─────────┘
                   │ routing success
                   ▼
              ┌──────────┐
              │  Routed  │
              └──────────┘

   Raw ──── enrichment empty result OR enrichment fault ──► Orphan
   Classified ── no integration OR routing fault ──► Unhandled
```

`Completed` is reserved for Block 4+ (post-persistence audit/cleanup); `Routed` is the terminal stage for Slice B.

## Error handling

| Failure | Behaviour |
|---|---|
| Consumer throws once | MassTransit retries per the per-consumer policy (D4) |
| Consumer throws past budget | `Fault<T>` published; observer updates `LifecycleStage` |
| Observer throws | Logged; bus drops message into secondary error queue; no further state change |
| Bus startup fails | `FlowHub.Web` startup fails fast — no degraded-mode operation |
| `IClassifier` returns null | Treated as defect; throws → fault → `Orphan` |

ProblemDetails (RFC 9457) is HTTP-scoped — not used on the bus. Logged exceptions are Serilog structured events with `CaptureId`, consumer name, `MessageId`, and exception details.

## Testing

`MassTransit.Testing` provides `ITestHarness` and consumer harnesses. Tests live in `tests/FlowHub.Web.ComponentTests/Pipeline/`.

### Test cases

1. **Enrichment publishes `CaptureClassified` for URL content.** Publish `CaptureCreated{Content="https://x.com"}` with the real `KeywordClassifier` registered → assert `CaptureClassified{MatchedSkill="Wallabag"}` was published; assert capture's `LifecycleStage == Classified`.
2. **Empty classification result lands in `Orphan` directly.** Publish `CaptureCreated{Content="lorem ipsum"}` → assert `CaptureClassified` was *not* published; assert `LifecycleStage == Orphan`.
3. **Routing flips capture to `Routed` on success.** Publish `CaptureClassified{MatchedSkill="Wallabag"}` with a stub `ISkillIntegration{Name="Wallabag"}` returning success → assert `LifecycleStage == Routed`.
4. **Unknown skill lands in `Unhandled`.** Publish `CaptureClassified{MatchedSkill="DoesNotExist"}` with no matching integration → assert `LifecycleStage == Unhandled`.
5. **Enrichment fault → `Orphan`.** Stub `IClassifier` to throw; publish `CaptureCreated`; wait for `Fault<CaptureCreated>` to be consumed; assert `LifecycleStage == Orphan`.
6. **Routing fault → `Unhandled`.** Stub `ISkillIntegration` to throw; publish `CaptureClassified`; wait for `Fault<CaptureClassified>`; assert `LifecycleStage == Unhandled`.
7. **Retry policy fires the right number of times.** `IClassifier` stub counts invocations; assert exactly 3 calls (1 initial + 2 retries) before fault.

Test naming: `Method_State_Behaviour` per CLAUDE.md.

### Test infrastructure pattern

```csharp
await using var provider = new ServiceCollection()
    .AddSingleton<ICaptureService, InMemoryCaptureService>()
    .AddSingleton<IClassifier>(new FakeClassifier(...))
    .AddMassTransitTestHarness(x =>
    {
        x.AddConsumer<CaptureEnrichmentConsumer>(c =>
            c.UseMessageRetry(r => r.Intervals(10, 10)));
        x.AddConsumer<SkillRoutingConsumer>(c =>
            c.UseMessageRetry(r => r.Intervals(10, 10, 10)));
        x.AddConsumer<LifecycleFaultObserver>();
    })
    .BuildServiceProvider(true);

var harness = provider.GetRequiredService<ITestHarness>();
await harness.Start();
```

Retry intervals are tightened in tests (`10ms`) so the suite stays under a second per case.

## Deployment topology (Block 5 preview)

For Block 3 we ship a `docker-compose.yml` *sketch* that demonstrates the multi-container option without making it the runtime default. Three services:

```yaml
services:
  flowhub.web:
    image: flowhub-web:dev
    environment:
      Bus__Transport: RabbitMq
      Bus__RabbitMq__Host: rabbitmq
    depends_on: [rabbitmq]

  flowhub.api:
    image: flowhub-api:dev    # lands in Slice A
    environment:
      Bus__Transport: RabbitMq
      Bus__RabbitMq__Host: rabbitmq
    depends_on: [rabbitmq]

  rabbitmq:
    image: rabbitmq:3-management-alpine
    ports:
      - "15672:15672"
```

This compose file is committed but **not** invoked by `make run` (which stays single-process via `dotnet run`). Block 5 fleshes out the full deployment story.

## Open questions / deferred work

- **Outbox pattern.** Postponed to Block 4 — depends on EF Core landing.
- **Idempotency receiver filter.** Postponed to Block 4 — depends on persistence.
- **`SkillRouted` event.** ADR 0002 mentioned it; we drop it from Slice B since routing is the terminal step. May resurface in Block 4 if persistence wants to record routing as a separate audit event.
- **Long-running classifier in Slice C.** When `AiClassifier` lands, the per-consumer retry intervals on `CaptureEnrichmentConsumer` may need to grow (an LLM call already takes seconds). Re-evaluate D4 in Slice C.
- **ADR 0003 itself.** Authored from this spec during execution; lives at `docs/adr/0003-async-pipeline.md`.

## References

- ADR 0001: `docs/adr/0001-frontend-render-mode-and-architecture.md`
- ADR 0002: `docs/adr/0002-service-architecture-and-async-communication.md`
- API surface sketch: `docs/design/api/api-surface.md`
- Block 3 Nachbereitung: `vault/Blöcke/03 Service/03 Service - c) Nachbereitung.md`
- POC: `poc/restful-api-playground/`
- Bewertungskriterien: `vault/Organisation/Bewertungskriterien.md`
- MassTransit docs: https://masstransit.io/documentation

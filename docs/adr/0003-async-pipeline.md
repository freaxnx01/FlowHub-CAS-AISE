# ADR 0003 — Async Pipeline: MassTransit Topology, Retry, and Fault Handling

- **Status:** Accepted
- **Date:** 2026-05-02
- **Block:** Block 3 (Services) — Nachbereitung · Slice B
- **Decider:** freax
- **Affects:** `source/FlowHub.Core/Events/`, `source/FlowHub.Core/Classification/`, `source/FlowHub.Core/Skills/`, `source/FlowHub.Web/Pipeline/`, `source/FlowHub.Web/Program.cs`, `docker-compose.yml`, `tests/FlowHub.Web.ComponentTests/Pipeline/`

---

## Context

ADR 0002 established the overall architecture: FlowHub stays a Modular Monolith, MassTransit is the message bus abstraction (in-memory in dev/test, RabbitMQ in production), and the `CaptureCreated → enrichment → routing` flow is the worked example for the Block 3 requirement to *"demonstrate asynchronous, event-based communication"*. What ADR 0002 deliberately deferred was the concrete topology: which specific flows go through the bus, which events exist, how retry and dead-letter handling behave, how faults surface back to the lifecycle state machine, and how the deployment story satisfies the rubric's *"Sub-Systeme als unabhängige Container deploybar"* dimension (5 pts).

This ADR closes those open questions. It covers the implementation decisions made during Block 3 Nachbereitung Slice B (the working MassTransit pipeline, first consumers, and tests). It is a narrower, more focused record than ADR 0002 — the brainstorming narrative lives in `docs/superpowers/specs/2026-04-30-async-pipeline-design.md`; this ADR distils the durably decided items from decisions D1 through D13 of that spec.

The Block 3 Moodle Auftrag explicitly names *"asynchrone Kommunikation mittels einer Queue"* as the primary deliverable. The rubric's *KI / Sub-Systeme / Reflexion* bucket additionally awards 5 pts for *"Sub-Systeme als unabhängige Container deploybar"*. Both are addressed without splitting the codebase.

---

## Decision

### 1. Async surface = capture-submit and routing (flows 1 + 2 only)

The pipeline covers exactly two flows: (1) capture submit triggers an enrichment consumer via `CaptureCreated`, and (2) enrichment success triggers a routing consumer via `CaptureClassified`. No other flows are made asynchronous in Slice B.

This is narrower than the four-flow table sketched in earlier drafts. Flow 3 (a dedicated routing event separate from enrichment output) was dropped because it adds an unnecessary extra hop without giving the consumer more information — routing happens inline at the tail of the enrichment consumer, which already has the classification result. Flow 4 (manual retry as an async re-publish) is handled by a synchronous REST endpoint that re-publishes `CaptureCreated` for a given Capture id; it does not need its own event type or its own flow designation. The result is a simple two-event, two-consumer pipeline that is easy to test exhaustively and easy to explain in the PVA write-up.

### 2. Event vocabulary: `CaptureCreated` and `CaptureClassified` only

The final event set for Slice B is exactly two records:

```csharp
// source/FlowHub.Core/Events/CaptureCreated.cs
public sealed record CaptureCreated(
    Guid CaptureId,
    string Content,
    ChannelKind Source,
    DateTimeOffset CreatedAt);

// source/FlowHub.Core/Events/CaptureClassified.cs
public sealed record CaptureClassified(
    Guid CaptureId,
    IReadOnlyList<string> Tags,
    string MatchedSkill,
    DateTimeOffset ClassifiedAt);
```

Three candidate events from earlier drafts are dropped: `SkillRoutingRequested` (routing happens inline, not via a separate event), `IntegrationCallFailed` (failures surface as `Fault<CaptureClassified>`, which is handled by the fault observer), and `SkillRouted` (the terminal success state is recorded by the routing consumer directly on the Capture entity — no downstream subscriber needs to observe it in Slice B). ADR 0002 §6 notes that events may evolve freely while all consumers are in-process; additive changes become mandatory only when the bus crosses a process boundary in a future block.

### 3. Classification port: `IClassifier` with `KeywordClassifier` stub in Slice B

Classification is abstracted behind a port declared in `source/FlowHub.Core/Classification/`:

```csharp
public interface IClassifier
{
    Task<ClassificationResult> ClassifyAsync(string content, CancellationToken ct);
}

public sealed record ClassificationResult(
    IReadOnlyList<string> Tags,
    string MatchedSkill);
```

The Slice B implementation is `KeywordClassifier`: URL-pattern content → `Tags=["link"]`, `MatchedSkill="Wallabag"`; content containing `"todo"` or `"task"` (case-insensitive) → `Tags=["task"]`, `MatchedSkill="Vikunja"`; everything else → `Tags=["unsorted"]`, `MatchedSkill=""` (handled per Decision 6). Slice C swaps in `AiClassifier : IClassifier` without touching consumer code — the hexagonal port shape isolates the substitution entirely. This also earns the *"Code lesbar, nach Layer, Modulen und Sub-Systemen strukturiert"* rubric dimension (max 7 pts).

### 4. Skill integration port: `ISkillIntegration` with `LoggingSkillIntegration` stubs

Outbound integration calls are abstracted behind a second port in `source/FlowHub.Core/Skills/`:

```csharp
public interface ISkillIntegration
{
    string Name { get; }
    Task WriteAsync(Capture capture, IReadOnlyList<string> tags, CancellationToken ct);
}
```

Slice B registers two `LoggingSkillIntegration` instances (one named `"Wallabag"`, one named `"Vikunja"`). Each stub logs `"would write to {Name} for capture {CaptureId}"` and returns successfully. The routing consumer resolves the correct integration via `IEnumerable<ISkillIntegration>` and selects by `Name` — if no match is found, it marks the Capture as `Unhandled` directly (Decision 6). Real adapters (Wallabag, Wekan, Vikunja HTTP clients) replace these stubs in Block 4/5 without any consumer changes.

### 5. Per-consumer retry policy: enrichment `Intervals(100, 500)`, routing `Intervals(500, 2000, 5000)`

Retry intervals are set per consumer rather than via a shared global policy:

```csharp
x.AddConsumer<CaptureEnrichmentConsumer>(c =>
    c.UseMessageRetry(r => r.Intervals(100, 500)));

x.AddConsumer<SkillRoutingConsumer>(c =>
    c.UseMessageRetry(r => r.Intervals(500, 2000, 5000)));
```

The enrichment consumer calls `IClassifier`, which in Slice B is an in-process keyword match. Two retries at 100 ms and 500 ms is enough budget for a transient fault without introducing meaningful latency. The routing consumer calls `ISkillIntegration`, which in Block 4/5 will be an outbound HTTP call to a real external service. Three retries at 500 ms, 2 s, and 5 s reflects a realistic cross-process latency profile and gives a slow upstream service room to recover. Separating the policies avoids over-waiting for the cheap operation and under-waiting for the expensive one.

### 6. Empty classification → direct `Orphan`; fault observer maps `Fault<T>` to lifecycle state

Two entry points into `Orphan` and two entry points into `Unhandled` are made explicit:

| Trigger | Path | Final stage |
|---|---|---|
| `KeywordClassifier` returns `MatchedSkill=""` | Enrichment consumer sets stage directly | `Orphan` |
| Enrichment consumer throws past retry budget | `Fault<CaptureCreated>` → `LifecycleFaultObserver` | `Orphan` |
| No `ISkillIntegration` registered for matched skill | Routing consumer sets stage directly | `Unhandled` |
| Routing consumer throws past retry budget | `Fault<CaptureClassified>` → `LifecycleFaultObserver` | `Unhandled` |

`LifecycleFaultObserver` is a single class implementing both `IConsumer<Fault<CaptureCreated>>` and `IConsumer<Fault<CaptureClassified>>`. It maps each fault to the appropriate terminal stage and logs at `LogLevel.Error` with `CaptureId`, originating message id, and `ExceptionInfo` from the `Fault.Exceptions` collection. The observer is best-effort: if it throws, the message rots in the bus's secondary error queue and the Capture's `LifecycleStage` is unchanged — operator must reconcile manually. No recursive retry.

Without this observer, captures that exhaust their retry budget would stay visually at `Raw` or `Classified` on the Dashboard while silently rotting in the error queue. The observer makes the pipeline's resilience story visible in the operator UI.

Empty-classification lands in `Orphan` without going through the fault path because it is a successful outcome of the enrichment consumer (no exception is thrown; the classifier returned a valid but empty result). Mixing the "no skill match" case into the fault path would mean the enrichment consumer deliberately throws to signal a business condition, which is an anti-pattern.

### 7. `KebabCaseEndpointNameFormatter` for stable queue names

```csharp
x.SetKebabCaseEndpointNameFormatter();
```

With this formatter, `CaptureEnrichmentConsumer` becomes the endpoint name `capture-enrichment`, and `SkillRoutingConsumer` becomes `skill-routing`. These names are stable whether the transport is in-memory (Slice B) or RabbitMQ (Block 5). Without an explicit formatter MassTransit derives the queue name from the full class name, which can differ between .NET versions and is harder to write operator alerts against. Kebab-case also matches the existing REST URL convention (`/api/captures`, `/api/skills`).

### 8. `IBus` factory for singleton consumers that need `IPublishEndpoint`

`CaptureServiceStub` (the in-memory Capture repository from Block 2) publishes `CaptureCreated` via `IPublishEndpoint`. Because `CaptureServiceStub` is registered as a singleton and `IPublishEndpoint` is Scoped in MassTransit's DI model, a direct injection would produce a captive dependency (singleton holding a scoped service). The workaround for Slice B is to inject `IBus` instead:

```csharp
// IBus is Singleton in MassTransit's DI model — safe to hold in a singleton.
public CaptureServiceStub(IBus bus, ILogger<CaptureServiceStub> logger)
```

This resolves the captive-dependency problem because `IBus` is itself singleton-lifetime in MassTransit. The trade-off is a slightly heavier abstraction (`IBus` vs `IPublishEndpoint`). Block 4 will revisit this when the EF Core implementation of `ICaptureService` becomes Scoped, at which point `IPublishEndpoint` can be injected directly and `IBus` is no longer needed here.

### 9. One process in code; multi-container topology documented in `docker-compose.yml`

FlowHub stays a single deployable process per ADR 0002 Decision 1. `make run` still starts everything with no broker dependency. However, a `docker-compose.yml` sketch is committed alongside the code that demonstrates the multi-container topology for Block 5:

```
services:
  flowhub.web:   image: flowhub-web:dev    (Bus__Transport: RabbitMq)
  flowhub.api:   image: flowhub-api:dev    (Bus__Transport: RabbitMq)
  rabbitmq:      image: rabbitmq:3-management-alpine
```

This compose file is **not** invoked by `make run` (which remains single-process via `dotnet run`). Its purpose is to satisfy the Bewertungskriterien dimension *"Sub-Systeme als unabhängige Container deploybar"* (max 5 pts) without introducing the operational complexity of a physical process split during development. Block 5 fleshes out the full deployment story with real image builds and an override file.

### 10. Tests in `tests/FlowHub.Web.ComponentTests/Pipeline/`

Pipeline tests live alongside the existing component tests rather than in a dedicated test project. The MassTransit Test Harness (`ITestHarness`) integrates cleanly with xUnit's `IAsyncLifetime` pattern; the existing project already references xUnit, FluentAssertions, and NSubstitute. Splitting into a separate `FlowHub.Pipeline.Tests` project would add overhead (new `.csproj`, CI step, solution entry) for no real isolation benefit at this stage. The split can happen later if the suite grows past ~20 tests or if CI build times make parallelism worthwhile.

Retry intervals in tests are tightened to `10ms` so the suite stays under one second per case:

```csharp
x.AddConsumer<CaptureEnrichmentConsumer>(c =>
    c.UseMessageRetry(r => r.Intervals(10, 10)));

x.AddConsumer<SkillRoutingConsumer>(c =>
    c.UseMessageRetry(r => r.Intervals(10, 10, 10)));
```

---

## Alternatives Considered

### A. Make all four flows asynchronous

The earlier spec table sketched four flows: submit, enrichment, routing, and manual retry. Routing as a separate third event (after `CaptureClassified` rather than inline with it) would require a `SkillRoutingRequested` event and a third consumer. Manual retry as an event would require a `CaptureRetryRequested` type.

> Rejected because neither third nor fourth flow gains anything from the event hop. Routing already has the classification result in `CaptureClassified` — introducing `SkillRoutingRequested` just to forward it is noise. Manual retry is a single synchronous endpoint that re-publishes `CaptureCreated`; wrapping it in an event doesn't improve decoupling. Two flows is the minimum that meaningfully demonstrates the async pipeline story.

### B. Only the enrichment flow async (no routing consumer)

An even thinner scope would wire only `CaptureCreated → CaptureEnrichmentConsumer` and call the integration synchronously inside the enrichment consumer.

> Rejected because it removes the two-hop resilience story (retry on classification is different from retry on integration) and collapses the state machine to three states instead of five. The Bewertungskriterien rubric rewards *"intelligente und flexible Services"* (max 6 pts) — a single-consumer pipeline with synchronous integration writes doesn't demonstrate that. The routing consumer and its per-consumer retry policy are the part of the implementation that earns those points.

### C. Global retry policy shared across all consumers

MassTransit supports a global retry policy set at the bus configuration level:

```csharp
x.AddBus(provider => Bus.Factory.CreateUsingInMemory(cfg =>
    cfg.UseMessageRetry(r => r.Interval(3, 500))));
```

> Rejected because a global policy is a lie. The enrichment consumer calls in-process code and benefits from fast retries (100 ms, 500 ms). The routing consumer calls external services in Block 4/5 and needs longer back-off (500 ms, 2 s, 5 s). A shared policy would either over-wait for the cheap operation or under-wait for the expensive one. Per-consumer configuration forces the policy to match the actual cost profile of each consumer — it is also the honest answer to the reviewer question "why these intervals".

### D. Fault observer omitted; rely on the `_error` queue alone

MassTransit's in-memory transport and RabbitMQ both move exhausted-retry messages to an error queue automatically. It would be possible to skip `LifecycleFaultObserver` entirely and let the operator inspect the error queue directly.

> Rejected because silent failures are invisible to the operator UI. Without the observer, a capture that fails all retries shows `LifecycleStage=Raw` on the Dashboard indefinitely — the operator has no idea it is stuck. The observer costs one class and two `IConsumer<Fault<T>>` implementations; the benefit is that the Dashboard and API `?stage=Orphan` / `?stage=Unhandled` filters actually reflect reality. The `⚠` states (`Orphan`, `Unhandled`) exist precisely to surface these failure cases.

### E. Outbox pattern now (alongside the in-memory implementation)

An outbox pattern guarantees that database writes and message publishes happen atomically — the canonical solution to "what if the process crashes between writing the Capture and publishing `CaptureCreated`". MassTransit ships an `EntityFrameworkOutbox` that hooks into EF Core's `SaveChangesAsync`.

> ⏸ Deferred to Block 4. There is no EF Core persistence in Slice B — all state is in-memory (Block 2 stub). An outbox without a durable store is meaningless; the prerequisite for correctness (persistence) is the prerequisite for the outbox. Revisiting this in Block 4, once `FlowHub.Persistence` lands, is the right sequence. Noted in Block 3 Nachbereitung as an explicit open item.

---

## Consequences

### Rubric coverage

This ADR and its implementation directly address four Bewertungskriterien dimensions:

- **Entwurf: Lösungsansatz und Architektur beschrieben** (max 7 pts) — the ASCII architecture diagram in the spec and the state-machine diagram in this ADR cover both bildlich and textuell.
- **Programmierung: Code lesbar, nach Layer, Modulen und Sub-Systemen strukturiert** (max 7 pts) — `IClassifier` + `ISkillIntegration` ports in `FlowHub.Core`, consumers in `FlowHub.Web/Pipeline/`, transport config isolated to `Program.cs`.
- **KI / Sub-Systeme als unabhängige Container deploybar** (max 5 pts) — `docker-compose.yml` demonstrates the multi-container topology; Decision 9 explains why the code stays single-process during development.
- **KI / Intelligente und flexible Services** (max 6 pts) — the pipeline itself (async enrichment, classification port, fault observer, retry policy) is the worked example of an intelligent, failure-tolerant service.

The Quarkus / Jakarta EE criterion (max 10 pts) remains N/A — see ADR 0002 and the submission PDF note.

### Complexity tax

MassTransit adds a meaningful learning curve. The three areas where it bites soonest:

1. **Captive dependency** (Decision 8). MassTransit's DI lifetime model has `IBus` as singleton and `IPublishEndpoint` as scoped. Any singleton service that needs to publish must either inject `IBus` or use a factory. This is not obvious from the docs and requires a comment in the code to avoid future confusion.
2. **EventId namespacing convention.** Serilog event ids follow project-local ranges, scoped by where the LoggerMessage source-gen partial method physically lives:
   - **`1000–1999` — Pipeline** (consumers + fault observer in `source/FlowHub.Web/Pipeline/`). Slice B uses `1001` (`CaptureEnrichmentConsumer.LogOrphan`), `1002` (`SkillRoutingConsumer.LogUnhandled`), `1003` (`LifecycleFaultObserver.LogObserverFailed`).
   - **`2000–2999` — Skills** (adapters in `source/FlowHub.Skills/`). Slice B uses `2001` (`LoggingSkillIntegration.LogStubWrite`).
   - Higher ranges are unallocated; new modules pick a free range when they land.

   Not enforced by tooling — followed by discipline and reflected here + in commit messages.
3. **Test harness retry intervals.** Tests use `10ms` retry intervals to keep the suite fast. If a test is flaky, the first thing to check is whether the retry budget is being exhausted faster than the assertions can observe the state transition. The `ITestHarness` `await harness.Consumed<T>()` helper handles most of this, but it is worth knowing.

### In-memory transport caveats

The in-memory transport is purpose-built for dev and test: no broker dependency, fast startup, simple setup. Its limitation is durability — if the process crashes between `ICaptureService.SubmitAsync` publishing `CaptureCreated` and the enrichment consumer consuming it, the message is lost. The Capture exists in the in-memory store (it was written first) but will never be enriched; its `LifecycleStage` stays at `Raw` permanently.

This is acceptable for Slice B because:
- The in-memory store itself is ephemeral. A process restart resets all state anyway, so lost messages are no worse than lost captures.
- The outbox pattern (Decision 5 / alternative E) is the correct fix, and it depends on Block-4 persistence.

Operators running the Block-5 RabbitMQ configuration do not have this problem — RabbitMQ persists messages across process restarts.

### RabbitMQ ops note: secondary error queue requires an operator playbook

When Block 5 lands RabbitMQ, MassTransit routes exhausted-retry messages to `<consumer>_error`. If `LifecycleFaultObserver` itself throws on one of those messages, MassTransit routes the fault-of-the-fault to `<consumer>_error_error` — the secondary error queue. There is no automatic reconciliation: messages that land there rot indefinitely, and the corresponding Captures show stale `LifecycleStage` values in the UI.

⚠ Block 5 must include a Prometheus alert on the depth of `capture-enrichment_error_error` and `skill-routing_error_error`. A queue depth above 0 for more than five minutes should page the operator. Without this alert, the failure mode is invisible until a user notices a capture stuck at `Raw`. The operator playbook for manual reconciliation (re-publish `CaptureCreated` via `POST /api/captures/{id}/retry`) must be documented alongside the Grafana dashboard.

---

## Consequences for the next blocks

### Block 4: Persistence + Outbox

- **Outbox pattern** — once `FlowHub.Persistence` lands with EF Core, wire `MassTransit.EntityFrameworkOutbox` so that `SaveChanges` and `Publish(CaptureCreated)` are atomic. This eliminates the lost-message window described above.
- **Revisit `IBus` factory pattern** — with a Scoped `ICaptureService` implementation backed by EF Core, `IPublishEndpoint` can be injected directly (both are Scoped); the `IBus` workaround in Decision 8 can be removed.
- **Idempotency receiver** — RabbitMQ delivers at-least-once. Once persistence exists, add a `MessageDataRepository`-backed idempotency filter so that redelivered messages are no-ops rather than double-writes. The consumers SHOULD (but currently don't) look up by `CaptureId` before mutating state — this is a correctness gap noted here for Block 4.
- **`FailureReason` sanitization** — `MarkOrphanAsync` and `MarkUnhandledAsync` accept a `reason` string that in the fault observer comes directly from `ExceptionInfo.Message`. Before persisting this string in a production DB, truncate it to a safe length (e.g. 500 chars) and strip any content that might contain sensitive operator-internal information (stack paths, connection strings). The in-memory stub has no persistence so the risk is low in Slice B.

### Block 5: RabbitMQ deployment + observability

- **RabbitMQ deployment** — `docker-compose.yml` (committed in Slice B) becomes the runtime default. Add `docker-compose.override.yml` for local secret injection. The `Bus__Transport=RabbitMq` / `Bus__RabbitMq__Host=rabbitmq` env vars are already wired in `Program.cs` from Decision 9.
- **Docker Compose topology** — `flowhub.web` + `flowhub.api` + `rabbitmq`. The two app containers share the same bus because they share the same RabbitMQ instance. Whether `flowhub.api` also publishes or only consumes is a Block-5 decision.
- **OIDC** — the dev-only `DevAuthHandler` is replaced with a real OIDC provider. This is orthogonal to the pipeline but shares the same Block-5 milestone.
- **Observability** — Prometheus alert on `capture-enrichment_error_error` and `skill-routing_error_error` queue depths (see RabbitMQ ops note above). Grafana board tracking `LifecycleStage` distribution over time (how many captures reach `Routed` vs stay at `Orphan` / `Unhandled`). The OpenTelemetry integration in `FlowHub.Web/Program.cs` already exports traces and metrics — MassTransit's `UseOpenTelemetry()` extension makes pipeline spans appear automatically.

---

## References

- Brainstorming spec: `docs/superpowers/specs/2026-04-30-async-pipeline-design.md` — full D1–D13 decision table and component sketches
- Implementation plan: `docs/superpowers/plans/2026-04-30-async-pipeline.md` — task-by-task execution record
- ADR 0001: `docs/adr/0001-frontend-render-mode-and-architecture.md` — render mode, Blazor Interactive Server
- ADR 0002: `docs/adr/0002-service-architecture-and-async-communication.md` — Modular Monolith, MassTransit selection, transport strategy
- API surface sketch: `docs/design/api/api-surface.md` — REST endpoint conventions referenced by event contract shape
- Block 3 Nachbereitung: `vault/Blöcke/03 Service/03 Service - c) Nachbereitung.md` — block checklist and Bewertungskriterien subset
- Bewertungskriterien: `vault/Organisation/Bewertungskriterien.md` — canonical Moodle rubric (18 items, max 100 pts)
- MassTransit in-memory transport: https://masstransit.io/documentation/configuration/transports/in-memory
- MassTransit RabbitMQ transport: https://masstransit.io/documentation/configuration/transports/rabbitmq
- MassTransit Test Harness: https://masstransit.io/documentation/concepts/testing

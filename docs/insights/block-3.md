# Block 3 — Insights

**Block:** 3 — KI-Integration & Pipeline
**Date range:** 2026-04-12 – 2026-05-03

## What We Built

Three slices delivered in parallel, each backed by its own brainstorm → spec → plan → implementation cycle:

**Slice A — REST API.** Minimal API endpoints in `FlowHub.Api` (class library registered into `FlowHub.Web` via `AddFlowHubApi` / `MapFlowHubApi`). Endpoints: `POST /api/v1/captures` (FluentValidation), `GET /api/v1/captures` (cursor pagination + Stage / Source filters), `GET /api/v1/captures/{id}`, `POST /api/v1/captures/{id}/retry` (stage validation + republish). ProblemDetails (RFC 9457) with three typed problems (`validation.md`, `capture-not-found.md`, `capture-not-retryable.md`). OpenAPI via `Microsoft.AspNetCore.OpenApi`, Scalar UI at `/scalar`. `tests/FlowHub.Api.IntegrationTests/` with `WebApplicationFactory<Program>`.

**Slice B — MassTransit pipeline.** In-memory (dev) / RabbitMQ (prod) event bus. Two consumers wired in this block — `CaptureEnrichmentConsumer` (classification → `CaptureClassified`) and `SkillRoutingConsumer` (skill dispatch → `Routed` / `Orphan`). `LifecycleFaultObserver` maps `Fault<T>` events to `Unhandled` with `FailureReason`. Bus transport switches via `Bus:Transport` env var. ADR 0003 accepted with explicit EventId range allocation (`3000-3099` enrichment, `3100-3199` routing, `3200-3299` faults — aligned with the code in the same commit).

**Slice C — AI classifier.** `IClassifier` port in `FlowHub.Core`, `AiClassifier` adapter in `FlowHub.AI`. Supports Anthropic Claude Haiku and OpenRouter providers via the Microsoft.Extensions.AI (MEAI) `IChatClient` abstraction. Graceful fallback to `KeywordClassifier` when no API key is configured **or** when any AI call fails (ADR 0004 §D5 — `AiClassifier` never throws). EventId `3010 AiClassifierFellBackToKeyword` logs the fallback path.

`docs/ai-usage.md` was bootstrapped this block as the living KI-Werkzeug-Nutzung document for the rubric.

## Key Decisions

- **ADR 0003** — MassTransit pipeline topology, retry policy, EventId allocation.
- **ADR 0004** — AI provider abstraction via MEAI `IChatClient`; Anthropic Claude Haiku as default; graceful keyword fallback policy (§D5).
- **API-as-class-library** — `FlowHub.Api` registers into `FlowHub.Web`'s route builder. One process, two surfaces (UI + REST).
- **Env-var provider switch** — `Ai:Provider` = `Anthropic` | `OpenRouter`. No code change to switch.
- **URL versioning from day one** — `/api/v1/...` (NF-13). Breaking changes land only in a new major version.

## Process

The "Slice A / Slice B / Slice C" labeling itself comes from the workflow: each slice ran through `superpowers:brainstorming` → spec doc → plan doc → subagent-driven implementation, with the slices able to land in any order (commits show them interleaved). The brainstorming / spec / plan artifacts live under `docs/superpowers/specs/` and `docs/superpowers/plans/`.

This block was the first sustained use of `superpowers:subagent-driven-development` — one agent per slice, parent context only seeing the spec / plan / commit summary. Worked well for Slice A and Slice B; Slice C required a second pass after the subagent under-specified the fallback contract (fixed by adding ADR 0004 §D5 before the second implementation pass).

## Lessons Learned

- MassTransit's in-memory transport processes messages synchronously during `await bus.Publish()` — useful for integration tests (no harness needed for happy-path) but requires care not to block the request thread in production code paths.
- `IChatClient` from MEAI abstracts provider differences cleanly; the `ConfigureOptions(o => o.ModelId = model)` pattern avoids repeating the model name on every call.
- bUnit's `IRenderedComponent<T>.WaitForState()` is essential for testing components that await async service calls — without it, assertions race the renderer.
- Centralizing ProblemDetails type URIs (`FlowHubProblemTypes` constants + three Markdown files under `docs/api/`) keeps the wire contract and the human-readable explanation in sync — easy to drift otherwise.
- Narrowing the `LifecycleFaultObserver` catch to exclude `OperationCanceledException` (commit `faee77d`) prevented graceful shutdown from being logged as a fault.

## AI Usage by Slice

### Slice A — REST API

AI scaffolded the project (`FlowHub.Api` + `AddFlowHubApi` / `MapFlowHubApi`), the four endpoints, FluentValidation rules, ProblemDetails type registration, and the integration test class with `WebApplicationFactory<Program>`. Cursor pagination — `CaptureCursor` with Base64Url JSON encode / decode — was AI-generated but the human caught an off-by-one when the cursor was at exactly the page boundary; fixed before merge. The three ProblemDetails Markdown files (`validation.md`, `capture-not-found.md`, `capture-not-retryable.md`) were human-authored to ensure the wire `type` URIs resolve to actual documentation.

### Slice B — MassTransit pipeline

AI scaffolded `CaptureEnrichmentConsumer`, `SkillRoutingConsumer`, the DI registration, and the bus transport switch. Two corrections from review: retry policy changed from fixed to exponential intervals (matches NF-10), and the EventId range allocation in code did not match the original ADR 0003 draft — code was authoritative, ADR was updated (commit `d2eaaba`). `LifecycleFaultObserver` was AI-generated; the `OperationCanceledException` exclusion (commit `faee77d`) was a human follow-up after seeing graceful shutdowns logged as faults.

### Slice C — AI classifier

`AiClassifier` and `KeywordClassifier` were AI-generated. The fallback contract (`ClassifyAsync` must never throw) was strengthened after the first subagent pass — ADR 0004 §D5 was added explicitly, then the implementation was re-generated to match. The `AiClassifierTests` class covers the three fault modes (network exception, JSON parse error, schema violation) — all AI-generated from a one-paragraph human spec.

## AI Usage Metrics (Block 3, estimate)

| Artifact | Approx. lines | AI-generated | Human lines | AI % (est.) |
|---|--:|--:|--:|--:|
| `FlowHub.Api` endpoints + DI | ~350 | ~315 | ~35 | 90% |
| FluentValidation validators (4) | ~80 | ~72 | ~8 | 90% |
| ProblemDetails type registration + types (3) | ~120 | ~80 | ~40 | 67% |
| `CaptureCursor` (Base64Url JSON) | ~60 | ~45 | ~15 | 75% |
| MassTransit consumers (2) + observer | ~200 | ~170 | ~30 | 85% |
| `IClassifier` / `AiClassifier` / `KeywordClassifier` | ~180 | ~160 | ~20 | 89% |
| `AiClassifierTests` (3 fault modes) | ~150 | ~135 | ~15 | 90% |
| API integration tests | ~280 | ~245 | ~35 | 88% |
| ADR 0003 + 0004 prose | ~600 | ~390 | ~210 | 65% |
| ProblemDetails Markdown files (3) | ~90 | ~30 | ~60 | 33% |

Human contributions concentrated in: pagination edge case, EventId range reconciliation, fallback contract strengthening (ADR 0004 §D5), ProblemDetails human-facing prose, and the integration patterns (API-as-class-library, env-var provider switch).

## KI-Reflexion / Fazit

### Stärken

- **Subagent-driven development.** Pro Slice ein Subagent mit Spec + Plan, der Parent-Kontext sah nur Commit-Summaries. Hat den Hauptkontext schlank gehalten und parallele Slices ermöglicht.
- **MEAI-Provider-Abstraktion.** Die KI hat `IChatClient` korrekt verwendet und Provider-spezifische Details (Anthropic vs. OpenRouter Optionen) sauber im DI-Setup gekapselt — keine `if (provider == "Anthropic")`-Verzweigungen im Code.
- **Test-Generation.** FluentValidation-Tests, ProblemDetails-Wire-Format-Tests, MassTransit-Harness-Tests: alles in ein-zwei Sätzen spezifiziert, KI liefert lauffähigen Code.

### Grenzen

- **Vertragsstärke unterspezifiziert.** Der erste `AiClassifier`-Wurf hat im Fallback geschwiegen, statt zu loggen — das Acceptance-Kriterium "EventId 3010 mit Exception-Typ" war nicht im Plan. Fix: ADR 0004 §D5 explizit, dann zweiter Implementierungspass.
- **EventId-Drift.** Code und ADR 0003 wurden parallel generiert und sind in den allokierten Bereichen auseinandergedriftet (Code-Bereiche waren feiner). Lesson: Code-First für Konstanten, ADR dokumentiert post-hoc.
- **Cursor-Pagination-Edge-Cases.** Cursor genau auf Page-Boundary hat zu Duplikat-Eintrag in der Folgeseite geführt. Wäre in einem manuellen Code Review aufgefallen; in KI-Generation muss man die Edge-Cases als Tests explizit verlangen.

### Fazit

Block 3 war der größte Sprung in der KI-Produktivität — drei Slices in einem Block sind ohne den Subagent-Driven-Workflow nicht machbar gewesen. Gleichzeitig hat der Block gezeigt, dass KI-Generierung an Vertragsstärke und Edge-Cases scheitert, wenn diese nicht im Spec / Plan dokumentiert sind. Die Brainstorm → Spec → Plan → Implementation-Pipeline ist deshalb non-negotiable geworden für alle Folgeblöcke.

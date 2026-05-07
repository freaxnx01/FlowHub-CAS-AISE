# Block 3 — Insights

**Block:** 3 — KI-Integration & Pipeline  
**Date range:** 2026-04-12 – 2026-05-03

## What We Built

Three parallel slices:

**Slice A — REST API:** Minimal API endpoints in `FlowHub.Api` (class library registered into `FlowHub.Web`). Endpoints: `POST /api/v1/captures`, `GET /api/v1/captures`, `GET /api/v1/captures/{id}`, `POST /api/v1/captures/{id}/retry`. FluentValidation at the boundary, ProblemDetails (RFC 9457), OpenAPI via `Microsoft.AspNetCore.OpenApi`, Scalar UI at `/scalar`.

**Slice B — MassTransit Pipeline:** In-memory (dev) / RabbitMQ (prod) event bus. Two consumers: `CaptureEnrichmentConsumer` (classification) and `SkillRoutingConsumer` (skill dispatch). `LifecycleFaultObserver` captures DLQ events. Bus transport switches via `Bus:Transport` env var.

**Slice C — AI Classifier:** `IClassifier` port in `FlowHub.Core`, `AiClassifier` adapter in `FlowHub.AI`. Supports Anthropic and OpenRouter providers via MEAI `IChatClient`. Graceful fallback to `KeywordClassifier` when no API key configured.

## Key Decisions

- **ADR 0004:** Anthropic Claude Haiku as default AI provider; OpenRouter as backup (cost)
- **API-as-class-library:** `FlowHub.Api` registers into `FlowHub.Web`'s route builder — no second process needed
- **Env-var provider switch:** `Ai:Provider` = `Anthropic` | `OpenRouter` — no code change to switch

## Lessons Learned

- MassTransit's in-memory transport processes messages synchronously during `await bus.Publish()` — useful for integration tests but requires care not to block the request thread
- `IChatClient` from MEAI abstracts provider differences cleanly; the `ConfigureOptions(o => o.ModelId = model)` pattern avoids repeating model name on every call
- bUnit's `IRenderedComponent<T>.WaitForState()` is essential for testing components that await async service calls

## AI Tooling

Used Claude Code for the MassTransit consumer scaffold and FluentValidation setup. The generated `CaptureEnrichmentConsumer` required correction of message retry policy (exponential intervals instead of fixed). AI-generated OpenAPI metadata annotations were accurate. All business logic written by hand.

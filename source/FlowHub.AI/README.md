# FlowHub.AI

**Layer:** Outbound adapter (driven side) implementing `FlowHub.Core`'s
`IClassifier` and `IEmbeddingService` ports with an LLM backend.

The "intelligent service" of FlowHub: it turns a raw capture into a routing
decision (matched skill + tags + enriched title) and produces embeddings for
semantic search. See ADR 0004 (AI integration) and ADR 0006 (vector search).

## Contents

- `AiClassifier.cs` — `IClassifier` implementation; calls the configured chat
  model, validates the structured response, and re-validates the matched skill
  against an allow-list. Falls back deterministically to the keyword classifier
  on any model failure (logged under a dedicated `EventId`).
- `AiEmbeddingService.cs` — `IEmbeddingService` implementation for vector search.
- `AiProvider.cs` / `AiServiceCollectionExtensions.cs` — provider abstraction and
  DI wiring; the active provider is selected by configuration (one env var swap).
- `AiPrompts.cs`, `AiClassificationResponse.cs` — prompt templates and the
  schema-bound response contract.
- `Enrichers/` — pluggable content enrichers (e.g. `ZitateEnricher`),
  dispatched by `EnricherDispatcher`.
- `AiBootLogger.cs`, `AiRegistrationOutcome.cs` — startup diagnostics describing
  which AI capabilities are active.

## Resilience

The classifier degrades gracefully: a model error, timeout, or quota exhaustion
hands off to the keyword classifier so a capture is always classified. Cost
guards are configured at the composition root.

## Tests

`tests/FlowHub.AI.IntegrationTests` (trait-gated — live-model tests are opt-in).

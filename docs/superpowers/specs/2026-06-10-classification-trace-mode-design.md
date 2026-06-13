# Design — Classification debug/trace mode

**Date:** 2026-06-10
**Issue:** [#35](https://github.com/freaxnx01/FlowHub-CAS-AISE/issues/35) — Add a debug/trace mode (LLM, timing, tokens, cost)
**Status:** Approved (brainstorm) → ready for plan

## Goal

Surface the classification internals of each capture for transparency and demos:
which classifier ran (AI provider + model, or the keyword/URL fallback), the
classify-call latency, prompt/completion token counts, and an estimated cost.
Shown as a read-only panel on the capture detail page (`/captures/{id}`), gated
by an environment variable so it can be enabled on the public demo VPS.

## Decisions (from brainstorm)

1. **Always capture + persist** the trace; the env var only toggles the *panel*.
   The data is free from the classify response, so it is recorded for every
   capture regardless of the demo flag.
2. **Cost** is computed at display time from a config-bound price map with
   built-in defaults (not persisted — derived data stays out of the DB).
3. **Persist** the raw trace via one EF migration (nullable owned entity).
4. **Scope:** classification only (matches the issue). Enrichment LLM calls
   (`QuotesEnricher`) are out of scope.

## Background — what's already available

- `AiClassifier.ClassifyAsync` already wraps the model call in a `Stopwatch`
  (latency) and uses MEAI `IChatClient.GetResponseAsync<T>`. The returned
  `ChatResponse<T>` exposes `.Usage` (`UsageDetails.InputTokenCount` /
  `.OutputTokenCount`, `long?`).
- `AiRegistrationOutcome` (in `AiServiceCollectionExtensions`) holds the active
  `Provider` (enum) + `Model` (string).
- On AI failure/`schema_violation`, `AiClassifier` falls back to
  `KeywordClassifier`; the trace must report the classifier that *produced the
  result* (keyword), not the failed AI attempt.
- Attachment captures **skip classification entirely** (the Paperless
  short-circuit from #37) → they have no trace.

## Data model

### Domain — `FlowHub.Core.Classification.ClassifierTrace`

```csharp
public sealed record ClassifierTrace(
    ClassifierKind Kind,        // Ai | Keyword
    int LatencyMs,
    string? Provider = null,    // "OpenRouter" | "Anthropic" — Ai only
    string? Model = null,       // e.g. "google/gemma-4-31b-it:free" — Ai only
    int? PromptTokens = null,   // Ai only (from Usage.InputTokenCount)
    int? CompletionTokens = null); // Ai only (from Usage.OutputTokenCount)

public enum ClassifierKind { Ai, Keyword }
```

- `ClassificationResult` (Core) gains a non-null `ClassifierTrace Trace` field
  (every classifier returns one).
- `Capture` (Core) gains `ClassifierTrace? ClassifierTrace` (null when the
  capture was routed without classification, e.g. attachments, or predates the
  migration).

### Persistence — owned entity (mirrors `Attachment`)

`CaptureEntity` gains a nullable owned `ClassifierTrace` mapped to columns:
`ClassifierTrace_Kind` (string), `_LatencyMs` (int), `_Provider` (string?),
`_Model` (string?), `_PromptTokens` (int?), `_CompletionTokens` (int?).
All-null owned reference ⇒ `null` trace (same pattern as `Attachment`).
One EF migration adds these columns.

## Data flow

1. **`KeywordClassifier.ClassifyAsync`** (Core): wrap body in a `Stopwatch`;
   return `ClassificationResult` with `Trace = new(ClassifierKind.Keyword, latencyMs)`.
2. **`AiClassifier.ClassifyAsync`**: on success, read `response.Usage`, build
   `Trace = new(ClassifierKind.Ai, latencyMs, provider, model, prompt, completion)`.
   The provider/model come from a small record injected at construction (derived
   from `AiRegistrationOutcome`) so the classifier needn't re-read config. On
   fallback, return the keyword classifier's result **as-is** (its keyword trace
   is the truthful "which classifier ran").
3. **`CaptureEnrichmentConsumer.Consume`**: pass `result.Trace` to
   `MarkClassifiedAsync(..., trace)`. The attachment short-circuit path passes no
   trace (stays null).
4. **`ICaptureService.MarkClassifiedAsync`** gains a trailing
   `ClassifierTrace? trace = null` parameter; `EfCaptureService` + the
   `CaptureServiceStub` persist it onto the capture. `EfCaptureRepository` maps it
   to/from the owned entity.
5. **`CaptureDetail` page** reads `capture.ClassifierTrace` and renders the panel
   when the gate is on (below).

## Cost estimation

`IClassificationCostEstimator.Estimate(string? model, int? promptTokens, int? completionTokens) → decimal?`
- Backed by a price map: built-in defaults plus `Ai:Pricing:<model>` overrides
  from configuration, each entry = input + output USD per 1,000,000 tokens.
- Built-ins: the demo model (`google/gemma-4-31b-it:free`) = 0/0; Anthropic
  Haiku = its real per-Mtok price. Unknown model ⇒ returns `null` (UI shows "—").
- Pure function over persisted tokens; called by the detail page at render.
  Lives in `FlowHub.AI` (pricing is an AI concern; `FlowHub.Web` already
  references `FlowHub.AI`) and is unit-tested in isolation. Interface in
  `FlowHub.Core` so the Web page depends on the abstraction.

## The env gate

- `DemoTraceOptions { bool Enabled }` bound from config section `Demo:Trace`
  (default `false`), registered in `Program.cs`, injected into `CaptureDetail`.
- `demo/docker-compose.yml` sets `Demo__Trace__Enabled: "true"` in the
  `flowhub.web` environment block (next to the existing `Demo__*` keys).
- Panel visibility rule on `/captures/{id}`:
  - gate **off** → nothing rendered (zero footprint).
  - gate **on** + `ClassifierTrace` present → trace panel (kind chip; provider/
    model; latency; prompt/completion tokens; estimated cost).
  - gate **on** + trace **null** → a one-line note: "Routed without LLM
    classification (e.g. file upload)".

## UI

A new `ClassifierTracePanel` component (in `Components/Shared/`) takes a
`ClassifierTrace?` + the estimated cost and renders a compact MudBlazor card:
- `MudChip` for the classifier kind (AI vs Keyword/URL fallback).
- Rows for provider/model, latency (`{ms} ms`), tokens (`{prompt} + {completion}`),
  and cost (`$0.0000` or "free" for 0, "—" when unknown).
Placed on `CaptureDetail.razor` after the Metadata block, behind the gate.
Keep business logic out of the `.razor`; the page code-behind resolves the gate
+ cost and passes plain values in.

## Testing (TDD)

- **KeywordClassifier**: result carries `Trace.Kind == Keyword`, `LatencyMs >= 0`,
  no tokens.
- **AiClassifier**: with a fake `IChatClient` returning a known `Usage`, the
  result's trace has `Kind == Ai`, the injected provider/model, and the mapped
  prompt/completion tokens; on a forced `schema_violation` it returns the keyword
  trace.
- **CostEstimator**: known model → expected $; free model → 0; unknown → null;
  null tokens → null.
- **Persistence**: `MarkClassifiedAsync` round-trips a trace through
  `EfCaptureRepository` (Testcontainers) and reads back equal; null trace stays null.
- **bUnit `ClassifierTracePanel` / CaptureDetail**: renders rows when gate on +
  trace present; renders the "no classification" note when trace null; renders
  nothing when gate off.
- Full `dotnet test` green.

## Out of scope

- Enrichment LLM telemetry (QuotesEnricher).
- A global trace list / dashboard aggregation (per-capture panel only).
- Live provider pricing lookups (static config map only).
- Streaming/per-token timing (single classify-call latency only).

## Risks / notes

- MEAI `Usage` may be null for some providers/responses → tokens persist as null,
  UI shows "—"; cost null. Handle gracefully.
- The provider/model injected into `AiClassifier` must reflect the *active*
  configured model (from `AiRegistrationOutcome`), not the `ChatOptions.ModelId`
  default, to stay correct if config changes.

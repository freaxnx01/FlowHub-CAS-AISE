# Slice C — AI Integration Design Spec

- **Date:** 2026-05-03
- **Block:** Block 3 (Services) — Nachbereitung · Slice C
- **Author:** freax (with Claude Code, Opus 4.7)
- **Status:** Approved (brainstorming complete; awaiting plan)
- **Drives ADR:** 0004 — KI-Integration in Services
- **Related:** ADR 0001 (Frontend), ADR 0002 (Service architecture), ADR 0003 (Async pipeline)

---

## Context

ADR 0003 §3 pre-committed a hexagonal port `IClassifier` in `source/FlowHub.Core/Classification/` and shipped a deterministic `KeywordClassifier` for Slice B. Both ADR 0003 and the Block-3-c Nachbereitung checklist explicitly name "Slice C swaps in an AI-backed adapter" as the next step. This spec locks the design decisions for that adapter so a TDD-driven implementation plan can be written from a stable target.

The Block-3 Moodle Auftrag explicitly demands "intelligente und flexible Services" using AI; the Bewertungskriterien dimension *"Intelligente / flexible Services mit KI gebaut"* (max 6 pts) is the explicit deliverable. The ADR-0004 line in `vault/Blöcke/03 Service/03 Service - c) Nachbereitung.md` adds *"Provider, Abstraktion, Prompt-/Cost-Strategie"* as the four sub-decisions to nail.

The course's nominal vocabulary (Spring-AI, Koog, Quarkus) is the teacher's chosen reference stack — students may freely use any other stack, and FlowHub uses .NET 10. This spec presents the .NET solution in its own terms, with no Spring-AI / Koog cross-walk. The Quarkus/Jakarta-EE rubric criterion remains **N/A** for this slice, same precedent as ADR 0002 / 0003.

---

## Decisions (locked via Q1–Q10)

### D1. Slice C scope = classifier + AI-generated title (one round-trip)

The AI does two jobs in one structured-output call: classify (`Tags`, `MatchedSkill`) and produce a short title (`Title`). No summary, no skill-suggestion queue, no embeddings, no agent loop. Estimated effort 2 days.

Rejected scope variants: minimal swap (no Title — leaves the rubric "intelligente Services" point under-served) and multi-feature enrichment (title + summary + tag-suggestions + skill-suggestions — eats into the remaining Block-3-c budget, conflicts with the DB-Skizze, NfA, typed-clients, and CHANGELOG items still open in `03 Service - c) Nachbereitung.md`).

### D2. Two adapters: Anthropic native + OpenRouter (OpenAI-compatible aggregator)

Both adapters implement `Microsoft.Extensions.AI.IChatClient`. Operator picks one at boot via `Ai__Provider=Anthropic|OpenRouter`. Other adapter is registered but inactive.

The two transports are deliberately different:

- **Anthropic** uses the native Anthropic API via the `Anthropic.SDK` NuGet package's MEAI-compatible `IChatClient` implementation. Vendor-specific features available (prompt caching via `cache_control: ephemeral`).
- **OpenRouter** uses `Microsoft.Extensions.AI.OpenAI` with `Endpoint = https://openrouter.ai/api/v1`. OpenRouter is OpenAI-API-compatible; the same package thus reaches hundreds of models (Anthropic, OpenAI, Google, Meta Llama, Mistral, Qwen, …) behind one adapter shape.

This earns the *"flexible"* adjective in "intelligente und flexible Services" — one adapter is a vendor-native SDK, the other is an aggregator gateway, both behind one interface.

### D3. Abstraction = `Microsoft.Extensions.AI` (MEAI)

The Block-3-c Nachbereitung explicitly demands "Microsoft.Extensions.AI **oder** Semantic Kernel als Abstraktion einbinden". MEAI is the right shape for FlowHub's single-shot classifier-with-typed-output use case:

- One interface (`IChatClient`).
- Schema-driven structured output via `CompleteAsync<TResponse>`.
- Decorator chaining (`UseOpenTelemetry`, `UseDistributedCache`, custom delegates) via `ChatClientBuilder`.
- Lightweight; no kernel/plugin/planner machinery.

Semantic Kernel is reserved for a hypothetical Block-5 agent loop (see Reflexion below) and is **not** introduced in Slice C.

### D4. Port shape = extend `ClassificationResult` with `Title?`

`IClassifier` stays one method:

```csharp
Task<ClassificationResult> ClassifyAsync(string content, CancellationToken cancellationToken);
```

`ClassificationResult` gains an optional third field:

```csharp
public sealed record ClassificationResult(
    IReadOnlyList<string> Tags,
    string MatchedSkill,
    string? Title);   // ← new in Slice C
```

`KeywordClassifier` returns `Title: null`. `AiClassifier` returns both populated (or falls back to keyword on AI failure → `Title: null`).

Single round-trip is meaningfully cheaper than two ports (`IClassifier` + `IEnricher`), and the model returns both fields naturally inside one schema. The "purist" objection — classification ≠ enrichment — is real but doesn't justify the 2× cost for Slice C. Block 4/5 can split the port if a real use case demands it.

### D5. Failure behaviour = graceful fallback to `KeywordClassifier`

`AiClassifier` wraps `KeywordClassifier` as a hard floor. The fallback triggers on either:

- **Any exception** thrown by `IChatClient.CompleteAsync<…>` — network (`HttpRequestException`), timeout (`TaskCanceledException`), parse / schema (`JsonException` and friends), or anything else (catch `Exception`, since MEAI's exact taxonomy varies between provider adapters).
- **Defensive post-validation**: even when the call returns successfully, `AiClassifier` checks that `MatchedSkill` is in `{"Wallabag","Vikunja",""}`. If not, treat as a failure (the schema should have prevented this, but never trust the model).

In either case the adapter:

1. Logs `Warning` (`EventId 3010 AiClassifierFellBackToKeyword`) with the failure reason (exception type or `"schema_violation"`) and elapsed milliseconds.
2. Calls `_keyword.ClassifyAsync(content, ct)` and returns its result with `Title=null`.
3. Does **not** rethrow.

Consequences:

- Capture is always classified — AI outage degrades quality, never availability.
- The MassTransit retry budget from ADR 0003 §5 stays reserved for genuine pipeline / bus failures.
- The fault-observer path (ADR 0003 §6) is not entered by AI errors.
- Strong narrative for the rubric *"flexible Services"* — flexibility = graceful degradation.

### D6. Default models: Anthropic Haiku 4.5 / OpenRouter Llama 3.1 70B Instruct

| Provider | Model | Default env | Rationale |
|---|---|---|---|
| Anthropic | `claude-haiku-4-5-20251001` | `Ai__Anthropic__Model` | Cheapest current Claude; rock-solid on JSON-schema responses; tool-use bridge for MEAI. |
| OpenRouter | `meta-llama/llama-3.1-70b-instruct` | `Ai__OpenRouter__Model` | Open-weights; reliable JSON-schema adherence at 70B (smaller variants flake on schema). Sets up the "commercial vs open-weights" rubric narrative. |

Tokens per call: ~200 input, ~150 output → cost is negligible regardless of provider; narrative dominates over cost. Both env vars are overridable for snapshot bumps.

### D7. Structured output = `CompleteAsync<T>` with JSON schema

```csharp
internal sealed record AiClassificationResponse(
    [property: Description("1–5 short lowercase tags describing the snippet")]
    string[] Tags,

    [property: Description("Wallabag, Vikunja, or empty string for none")]
    [property: AllowedValues("Wallabag", "Vikunja", "")]
    string MatchedSkill,

    [property: Description("3–8 word title or null if content is too short")]
    string? Title);
```

MEAI generates the JSON schema from the record. `AllowedValues` translates to a JSON-schema `enum` constraint; both Anthropic (via tool-use under the hood) and OpenRouter (via `response_format: json_schema`) honour it.

`MatchedSkill` MUST match a registered `ISkillIntegration.Name` from ADR 0003 §4 (`"Wallabag"`, `"Vikunja"`, or `""` → `Orphan`) or routing breaks downstream. Defensive runtime check inside `AiClassifier` re-validates the field even though the schema enforces it — belt-and-braces against future model misbehaviour.

### D8. No-key startup = silent fallback to `KeywordClassifier`

`AddFlowHubAi(IConfiguration)` (extension method in `source/FlowHub.AI/AiServiceCollectionExtensions.cs`) inspects `Ai__Provider` and the matching `ApiKey`. Behaviour matrix:

| `Ai__Provider` | `Ai__<P>__ApiKey` | DI registration | Startup log |
|---|---|---|---|
| unset | — | `KeywordClassifier` as `IClassifier` | Info `3021 AiProviderNotConfigured` |
| `Anthropic` | unset | `KeywordClassifier` as `IClassifier` | Info `3021 AiProviderNotConfigured` (with reason) |
| `Anthropic` | set | `AiClassifier` as `IClassifier`; `KeywordClassifier` as itself (for fallback) | Info `3020 AiProviderRegistered` (provider, model) |
| `OpenRouter` | unset | `KeywordClassifier` as `IClassifier` | Info `3021 AiProviderNotConfigured` (with reason) |
| `OpenRouter` | set | `AiClassifier` as `IClassifier`; `KeywordClassifier` as itself (for fallback) | Info `3020 AiProviderRegistered` (provider, model) |
| invalid value | — | throws `InvalidOperationException` at startup | — |

`make run` works zero-config for a fresh `git clone` — same dev-friendly philosophy as `DevAuthHandler` for auth.

### D9. Testing = mocked unit tests + trait-gated integration tests

**Unit tests** (`tests/FlowHub.Web.ComponentTests/Ai/AiClassifierTests.cs`, ~10 cases) — all via `NSubstitute.For<IChatClient>()` and `NSubstitute.For<IClassifier>()` for the keyword floor. Zero real API calls.

Coverage:

1. Returns AI result when `IChatClient.CompleteAsync<…>` succeeds with valid schema.
2. Forwards `CancellationToken` to `IChatClient`.
3. Sets `MaxOutputTokens=300` on `ChatOptions`.
4. Sets `Temperature=0.2` on `ChatOptions`.
5. Falls back to `KeywordClassifier` when `IChatClient` throws `HttpRequestException`.
6. Falls back when `IChatClient` throws `TaskCanceledException` (timeout).
7. Falls back when `IChatClient` throws `JsonException` (schema parse failure).
8. Falls back when `MatchedSkill` is outside `{"Wallabag","Vikunja",""}` (defensive guard).
9. Falls back when `IChatClient` throws an unclassified `Exception` (catch-all path).
10. Logs `EventId 3010` at `Warning` on each fallback with exception type + duration.

**Integration tests** (`tests/FlowHub.AI.IntegrationTests/`, new project, ~4 cases, `[Trait("Category","AI")]`) — real provider calls, excluded from default `dotnet test`.

Coverage:

1. Anthropic Haiku 4.5: classify URL → `MatchedSkill="Wallabag"`, non-null `Title`.
2. Anthropic Haiku 4.5: classify "todo: buy milk on Saturday" → `MatchedSkill="Vikunja"`, non-null `Title`.
3. OpenRouter Llama 3.1 70B: same two assertions as 1+2.
4. Schema-shape guard: assert all responses round-trip cleanly through the JSON-schema validator (catches model regression).

`Makefile` additions:

```make
test:        ## run all tests except [Category=AI]
	dotnet test FlowHub.slnx --filter "Category!=AI"

test-ai:     ## run integration tests against real AI providers (requires Ai__*__ApiKey env)
	dotnet test tests/FlowHub.AI.IntegrationTests --filter "Category=AI"
```

CI runs only `make test`. `make test-ai` is operator-on-demand.

### D10. Active provider selection = explicit `Ai__Provider` env var

One env var swaps providers. No implicit precedence; no per-request runtime selection; no round-robin. Demoing "swap providers via config" is a one-line env change.

---

## Architecture & component map

```
                      ┌────────────────────────────────────────────────────────┐
                      │  source/FlowHub.Web/Program.cs                         │
                      │  builder.Services.AddFlowHubAi(builder.Configuration); │
                      │     ─ if no provider/key  → KeywordClassifier wins     │
                      │     ─ if provider+key set → AiClassifier wins          │
                      └────────────┬───────────────────────────────────────────┘
                                   │ DI
                                   ▼
   ┌────────────────────────────────────────────────────────────────────────────┐
   │  source/FlowHub.AI/  (was placeholder — now active)                        │
   │                                                                            │
   │  AiClassifier : IClassifier                                                │
   │     ─ ctor: IChatClient, KeywordClassifier (floor), ILogger, ChatOptions   │
   │     ─ ClassifyAsync: try AI → catch → fall back to keyword                 │
   │  AiClassificationResponse  ── DTO with JSON-schema attributes              │
   │  AiPrompts                 ── system + user prompt strings                 │
   │  AiServiceCollectionExtensions.AddFlowHubAi(IConfiguration)                │
   └───────────────────────────────┬────────────────────────────────────────────┘
                                   │ IChatClient (MEAI)
                  ┌────────────────┴────────────────┐
                  ▼                                 ▼
        ┌────────────────────┐            ┌────────────────────┐
        │ Anthropic.SDK      │            │ Microsoft.Extensions│
        │ (MEAI-compatible   │            │ .AI.OpenAI w/ custom│
        │  IChatClient)      │            │ Endpoint            │
        │ Haiku 4.5          │            │ Llama 3.1 70B       │
        │                    │            │  (via OpenRouter)   │
        └────────────────────┘            └────────────────────┘
```

### Module split

| Lives in | What | Why |
|---|---|---|
| `source/FlowHub.Core/Classification/` | `IClassifier`, `ClassificationResult` (extended with `Title?`), `KeywordClassifier` | Domain port stays in Core. Slice C only adds a field to the result record. |
| `source/FlowHub.AI/` (was placeholder) | `AiClassifier`, `AiClassificationResponse`, `AiPrompts`, `AiServiceCollectionExtensions` | New code lives in its own project. Mirrors the `FlowHub.Skills/` and `FlowHub.Integrations/` topology for later blocks. CLAUDE.md placeholder note gets updated. |
| `tests/FlowHub.Web.ComponentTests/Ai/` | 10 mocked unit tests | Reuses the existing test project per ADR 0003 §10 rationale (no new csproj needed for unit tests). |
| `tests/FlowHub.AI.IntegrationTests/` (new) | 4 trait-gated real-provider tests | New project — keeps live API tests isolated from CI default. |

### DI lifetimes

- `IChatClient` → **Singleton** (wraps `HttpClient` via typed-client pattern; thread-safe per MEAI docs).
- `AiClassifier` → **Singleton** (consumes Singletons; no captive-dependency trap).
- `KeywordClassifier` → **Singleton** (already today).

No analogue of the ADR 0003 §8 `IBus` factory pattern is needed here.

---

## Data flow

### Happy path (Anthropic configured)

```
1. CaptureServiceStub.SubmitAsync → publishes CaptureCreated
2. CaptureEnrichmentConsumer.Consume(CaptureCreated)
3.   ├─ resolves IClassifier → AiClassifier
4.   ├─ AiClassifier.ClassifyAsync(content, ct):
5.   │     ├─ chatClient.CompleteAsync<AiClassificationResponse>(messages, options, ct)
6.   │     │     ├─ MEAI generates JSON schema from AiClassificationResponse
7.   │     │     ├─ Anthropic adapter sends tool-use call w/ schema
8.   │     │     └─ provider returns structured response
9.   │     ├─ defensive: validate MatchedSkill ∈ {"Wallabag","Vikunja",""}
10.  │     └─ returns ClassificationResult(tags, skill, title)
11.  ├─ if MatchedSkill="" → mark Capture Orphan (existing path)
12.  └─ else publish CaptureClassified (existing flow → routing consumer)
```

### Failure path

```
4'.  AiClassifier.ClassifyAsync(content, ct):
5'.    Stopwatch.Start()
6'.    try:
7'.      result = await chatClient.CompleteAsync<…> with 10s timeout + 0 internal retries
8'.      if result.MatchedSkill ∉ {"Wallabag","Vikunja",""}:
9'.        throw new InvalidOperationException("schema_violation: " + result.MatchedSkill)
10'.   catch (Exception ex):                              ← catch-all, MEAI taxonomy varies
11'.     ├─ log Warning EventId 3010 with reason (ex.GetType().Name or "schema_violation")
12'.     │                                + elapsed_ms (sanitised)
13'.     └─ return _keyword.ClassifyAsync(content, ct)    ← deterministic floor; Title=null
14'.   // capture is always classified — AI outage degrades quality, not availability
```

The catch is intentionally broad. MEAI exception types are stable for the abstractions but the per-adapter HTTP/JSON pipeline may surface provider-specific exception types we don't want to enumerate. Tests exercise the canonical ones (`HttpRequestException`, `TaskCanceledException`, `JsonException`) plus a generic `Exception` to lock in the catch-all behaviour.

`AiClassifier` does not rethrow on AI failure (D5). Pipeline retries (ADR 0003 §5) and the fault-observer path (ADR 0003 §6) are reserved for genuine pipeline failures.

---

## Configuration surface

| Env var | Required when | Default | Notes |
|---|---|---|---|
| `Ai__Provider` | always (else keyword-only) | unset | `Anthropic` or `OpenRouter`; case-insensitive |
| `Ai__Anthropic__ApiKey` | `Provider=Anthropic` | — | `sk-ant-…` |
| `Ai__Anthropic__Model` | optional | `claude-haiku-4-5-20251001` | overridable for snapshot bumps |
| `Ai__OpenRouter__ApiKey` | `Provider=OpenRouter` | — | OpenRouter key |
| `Ai__OpenRouter__Model` | optional | `meta-llama/llama-3.1-70b-instruct` | any OpenRouter slug |
| `Ai__OpenRouter__Endpoint` | optional | `https://openrouter.ai/api/v1` | escape hatch |
| `Ai__MaxOutputTokens` | optional | `300` | defensive cap; schema needs ~150 |
| `Ai__TimeoutSeconds` | optional | `10` | per AI call; 0 disables timeout |

**Where keys live in dev**: ASP.NET User Secrets — `dotnet user-secrets set Ai:Anthropic:ApiKey sk-ant-…` for `make run`. Environment variables / Docker secrets in production. Never `appsettings.Development.json` (gitignored, but force-add risk per ADR 0003 §10 lessons).

---

## Prompt strategy

**System prompt** — English (German-only system prompts confuse Llama 3.1 on routing-vocabulary terms; capture content can still be German):

```
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
```

**User message** (per capture): the raw `content` string, no wrapping.

**No few-shot examples**: schema enforcement makes few-shot redundant for `MatchedSkill`. Total input stays under ~500 tokens. If quality issues surface in the D9 integration tests, few-shot can be added without touching the port shape.

**Where the prompt lives**: `internal static class AiPrompts` in `source/FlowHub.AI/AiPrompts.cs` — system prompt as a `const string`; `BuildMessages(content)` helper. Easy to grep, easy to diff in code review.

---

## Cost guards

- `ChatOptions.MaxOutputTokens = 300` — schema produces ~150 output tokens; cap leaves margin.
- `ChatOptions.Temperature = 0.2` — deterministic-ish; classifications shouldn't drift between runs.
- HTTP timeout = `TimeSpan.FromSeconds(10)` on the underlying `HttpClient` — enforced before `CompleteAsync` returns.
- **Anthropic prompt cache**: system prompt marked `cache_control: ephemeral` via `ChatOptions.AdditionalProperties["cache_control"]`. ~80% input-token discount on the system prompt segment after the second call.
- **OpenRouter prompt cache**: not universally supported across upstream models; Slice C does not claim it. Documented asymmetry — a real difference between adapters worth surfacing in the ADR.

---

## Observability

**EventId range 3000–3999 — AI** (extends ADR 0003 namespace convention: 1000–1999 Pipeline, 2000–2999 Skills, 3000–3999 AI).

| EventId | Logger method | Level | When |
|---|---|---|---|
| 3001 | `AiClassifierStarted` | Debug | Before `CompleteAsync` |
| 3002 | `AiClassifierSucceeded` | Debug | Success — `provider`, `model`, `duration_ms`, `input_tokens`, `output_tokens` |
| 3010 | `AiClassifierFellBackToKeyword` | Warning | Fallback triggered — `reason` (exception type or `"schema_violation"`) + `duration_ms` |
| 3020 | `AiProviderRegistered` | Information | Startup — provider was registered (provider, model) |
| 3021 | `AiProviderNotConfigured` | Information | Startup — `KeywordClassifier` is the registered `IClassifier` (with reason: missing-provider / missing-key) |

`LoggerMessage` source-gen for all five (CA1848 / CA1873 already enforced by `Directory.Build.props`).

**OpenTelemetry**: `Microsoft.Extensions.AI` ships a `UseOpenTelemetry()` decorator on `IChatClient` that emits `gen_ai.client.operation.duration` and token-count metrics on the existing OTEL pipeline. Wired inline in `AddFlowHubAi`. Surfaces in the same Grafana dashboard as MassTransit traces (Block-5 wiring).

---

## Reflexion: `Microsoft.Extensions.AI` vs Semantic Kernel

The Block-3-c Nachbereitung explicitly demands a reflection on the two .NET AI frameworks. Slice C ships MEAI; here is the reasoning, sized for the submission PDF.

**`Microsoft.Extensions.AI`** is a thin abstraction over chat / embedding / tool-use, similar in spirit to `Microsoft.Extensions.Logging` or `Microsoft.Extensions.Caching`. One interface (`IChatClient`), schema-driven structured output (`CompleteAsync<T>`), and decorator chaining for cross-cutting concerns (telemetry, caching, function invocation). It gets out of your way when the call site is "give the model a string, get a typed object back".

**Semantic Kernel** is a full agent framework: a `Kernel` with plugins, planners, persistent memory, and an agent runtime. It's what you reach for when an LLM **orchestrates** multi-step work — e.g. an agent that retries failed integration calls, decides between alternatives, or walks a document collection answering questions. SK consumes `IChatClient` natively, so it is additive on top of MEAI, not a replacement.

**Decision rule for FlowHub:**

- **Single-shot LLM call with typed output → MEAI.** Slice C is exactly this.
- **Multi-step LLM-driven workflow → add Semantic Kernel** without removing MEAI.

A credible Block-5 fit for SK would be an **intelligent retry advisor** agent: when an integration call fails repeatedly, an SK agent inspects the failure pattern, capture content, and integration-health history, then decides between (re-route to a different skill, summarise and surface to the operator, escalate to manual). That is a multi-step decision loop — exactly what SK is designed for. Slice C deliberately stops short.

---

## .NET AI-stack landscape

The MEAI building blocks FlowHub may use, scaled from Slice C onward:

| Building block | What it is | Where Slice C uses it |
|---|---|---|
| `IChatClient` | Provider-agnostic chat/completion interface. Each provider ships an adapter (Anthropic.SDK, OpenAI, Azure OpenAI, Ollama, …). | The single seam in `AiClassifier`. Adapters for Anthropic and OpenRouter both implement it. |
| `CompleteAsync<T>` | Schema-driven structured output. Generates JSON schema from a C# record/class; provider returns typed `T` instead of raw text. | `AiClassificationResponse` record with `[AllowedValues]` enum on `MatchedSkill`. |
| `ChatOptions` | Per-call knobs: `MaxOutputTokens`, `Temperature`, tool-use, `AdditionalProperties` for vendor extensions. | `MaxOutputTokens=300`, `Temperature=0.2`, Anthropic-specific `cache_control: ephemeral`. |
| `IChatClient` decorators | `UseOpenTelemetry()`, `UseDistributedCache()`, `UseFunctionInvocation()`, custom delegates. Composable via `ChatClientBuilder`. | `UseOpenTelemetry()` for `gen_ai.*` metrics. |
| `IEmbeddingGenerator<TInput,TEmbedding>` | Embedding abstraction. Mirrors `IChatClient` but for vectors. | **Not in Slice C.** Reserved for Block 5 KI-search (pgvector via Npgsql). |
| `AIFunction` / `AIFunctionFactory.Create` | Tool/function-calling — wraps a C# method as a callable LLM tool. | **Not in Slice C** — typed-schema response covers this slice's needs. Potential Block-5 fit. |

For Slice C the abstraction depth is intentionally shallow: one `IChatClient`, one `CompleteAsync<T>` call per capture, one `UseOpenTelemetry()` decorator. No memory, no agent loop, no tool use, no embeddings.

---

## Rubric coverage

This spec + its implementation directly target six Bewertungskriterien dimensions:

- **Entwurf: Lösungsansatz und Architektur beschreiben** (max 7) — ASCII component diagram, data-flow diagram, MEAI building-block table.
- **Programmierung: Code lesbar, nach Layer/Modulen strukturiert** (max 7) — `FlowHub.AI` becomes a real project with single responsibility; `IClassifier` port stays in Core; adapters separated from prompt/DTO.
- **Programmierung: Erkenntnisse aus der Programmierung dokumentiert** (max 3) — ADR 0004 + a Slice-C section in `docs/ai-usage.md`.
- **Validierung: Unit-Tests programmiert** (max 3) — 10 mocked unit tests + 4 trait-gated integration tests.
- **KI: Wurden KI-Werkzeuge verwendet und deren Nutzung beschrieben** (max 12) ⭐ — partially earned by Slices A/B (KI-as-development-tool); this slice adds *production-runtime* AI use, which is what the rubric actually asks.
- **KI: Intelligente / flexible Services mit KI gebaut** (max 6) — explicit deliverable. Two adapters demonstrate flexibility; graceful keyword-fallback demonstrates "intelligent" failure handling.

The Quarkus/Jakarta-EE programming criterion (max 10) remains **N/A** for FlowHub's .NET stack — same precedent as ADR 0002 / 0003 and the Block-3-c header.

---

## Alternatives considered (rejected during Q1–Q10)

- **A. Semantic Kernel as the abstraction.** Rejected at Q3 — over-frameworking a single-shot classifier. Documented as the right pick if FlowHub grows an agent loop.
- **B. Local Ollama as one of the two providers.** Rejected at Q2 — user picked OpenRouter for narrative breadth + zero infrastructure dependency. Ollama remains a future option (it speaks OpenAI-compatible, so the same OpenRouter adapter works against an Ollama endpoint).
- **C. Sibling port `IEnricher` for the Title field.** Rejected at Q4 — single AI round-trip is cheaper and the model returns both fields naturally.
- **D. Hard-fail on AI errors (no fallback).** Rejected at Q5 — availability beats failure-signal purity; degraded-quality classification beats no classification.
- **E. Recorded-fixture (VCR-style) integration tests.** Rejected at Q9 — fixtures freeze prompts; trait-gated live tests catch real regressions for the same plumbing cost.
- **F. Implicit provider preference (Anthropic if both keys set).** Rejected at Q10 — one explicit `Ai__Provider` env var is easier to demo and less surprising.

---

## Consequences for next blocks

**Block 4 (Persistence)**:
- Embedding pipeline preparation: `IEmbeddingGenerator<string, Embedding<float>>` is already exposed by MEAI, so the embedding work for Block 5 KI-search (pgvector via Npgsql) is incremental, not greenfield.
- AI audit fields on the persisted `Capture`: `(provider, model, duration_ms, was_fallback)` per classification. Earns the *"Test-Ergebnisse dokumentiert"* rubric item with real production data.

**Block 5 (Deployment + KI-search)**:
- ADR 0006 (KI-Suche) builds on the `IEmbeddingGenerator` shape. Anthropic doesn't ship embeddings, so the embedding provider becomes either OpenAI native, OpenRouter (passing through to an OpenAI / Cohere / Voyage embedding model), or a self-hosted sentence-transformer behind an OpenAI-compatible facade. Asymmetry to document there.
- Integration-test secret rotation: `Ai__Anthropic__ApiKey` / `Ai__OpenRouter__ApiKey` move from User Secrets into the Block-5 deployment secret store (Authentik or Docker secrets, depending on Block-5 ADR).
- OTEL: `gen_ai.*` metrics from `UseOpenTelemetry()` already export. Grafana panel for "AI calls / sec, p95 latency, fallback rate" comes nearly free.
- Potential Semantic-Kernel adoption: an "intelligent retry advisor" agent (see Reflexion) is a credible Block-5 stretch. SK would be additive — MEAI stays as the chat-transport substrate.

---

## Implementation outline (for the plan)

The plan author should produce TDD-ordered tasks roughly along these lines (final plan owns the exact ordering):

1. Add NuGet packages to `Directory.Packages.props`: `Microsoft.Extensions.AI`, `Microsoft.Extensions.AI.OpenAI`, `Anthropic.SDK` (whichever ships the MEAI bridge), `Microsoft.Extensions.AI.Abstractions` (transitively).
2. Convert `source/FlowHub.AI/` placeholder to a real csproj; reference from `FlowHub.Web`.
3. Extend `ClassificationResult` with `Title?`. Update `KeywordClassifier` to return `Title=null`. Update Slice-B tests to match the new shape (additive — `Title` defaults make it backward-compatible).
4. Write `AiClassificationResponse` record with JSON-schema attributes. (Test: schema-generation round-trip.)
5. Write `AiPrompts` static class.
6. Write `AiClassifier` consuming `IChatClient` + `KeywordClassifier`. (Tests: 10 mocked unit tests per D9.)
7. Write `AiServiceCollectionExtensions.AddFlowHubAi(IConfiguration)` with the D8 behaviour matrix. (Tests: registration matrix verified via `IServiceProvider`.)
8. Wire `AddFlowHubAi` into `Program.cs`. Update `AddFlowHubAi` callers in tests to register the keyword path.
9. Add `tests/FlowHub.AI.IntegrationTests/` csproj, register in `FlowHub.slnx`. Implement the 4 trait-gated tests.
10. Add `make test` filter and `make test-ai` target.
11. Update `docs/ai-usage.md` with a Slice-C section (Reflexion: ✅/⚠/❌, generated-vs-handwritten share, notable adaptations).
12. Write ADR 0004 from this spec (distil to 7–10 numbered decisions, mirror the ADR 0003 shape).
13. Update CLAUDE.md `FlowHub.AI` placeholder note (it's no longer a placeholder).
14. Update `vault/Blöcke/03 Service/03 Service - c) Nachbereitung.md` — tick the relevant Slice-C lines.
15. Final `dotnet test` (default filter) green; build with `warnings-as-errors` clean; CHANGELOG `[Unreleased]` Slice-C entry.

The plan should also pin Sonnet 4.6 for TDD/judgment tasks and Haiku for mechanical tasks (csproj scaffolding, vault checklist), per project memory.

---

## References

- ADR 0001: `docs/adr/0001-frontend-render-mode-and-architecture.md`
- ADR 0002: `docs/adr/0002-service-architecture-and-async-communication.md`
- ADR 0003: `docs/adr/0003-async-pipeline.md` — `IClassifier` port, EventId namespacing, ADR template
- Block 3 Nachbereitung: `vault/Blöcke/03 Service/03 Service - c) Nachbereitung.md`
- Bewertungskriterien: `vault/Organisation/Bewertungskriterien.md`
- AI Usage living doc: `docs/ai-usage.md`
- `Microsoft.Extensions.AI` docs: https://learn.microsoft.com/en-us/dotnet/ai/microsoft-extensions-ai
- OpenRouter API reference: https://openrouter.ai/docs

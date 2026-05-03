# ADR 0004 — AI Integration in Services: Provider, Abstraction, Prompt + Cost Strategy

- **Status:** Accepted
- **Date:** 2026-05-03
- **Block:** Block 3 (Services) — Nachbereitung · Slice C
- **Decider:** freax
- **Affects:** `source/FlowHub.AI/`, `source/FlowHub.Core/Classification/ClassificationResult.cs`, `source/FlowHub.Web/Program.cs`, `Directory.Packages.props`, `tests/FlowHub.AI.IntegrationTests/`

---

## Context

ADR 0003 §3 pre-committed a hexagonal `IClassifier` port and shipped a deterministic
`KeywordClassifier` for Slice B. Both ADR 0003 and the Block 3-c Nachbereitung
checklist explicitly name "Slice C swaps in an AI-backed adapter" as the next step.
The Block 3 Moodle Auftrag demands "intelligente und flexible Services" using AI;
the Bewertungskriterien dimension *"Intelligente / flexible Services mit KI gebaut"*
(max 6 pts) is the explicit deliverable, and the highest-weighted single rubric item
(*"Wurden KI-Werkzeuge verwendet und deren Nutzung beschrieben"*, max 12 pts) demands
**production-runtime** AI use, not just AI as a coding assistant.

This ADR records the durable decisions for that adapter. The brainstorming narrative
lives in `docs/superpowers/specs/2026-05-03-slice-c-ai-integration-design.md`; this
ADR distils D1–D10 from that spec.

The Quarkus / Jakarta-EE programming criterion remains **N/A** for FlowHub's .NET
stack — same precedent as ADR 0002 / 0003. The course's nominal Spring-AI / Koog
vocabulary is the teacher's reference stack; FlowHub's .NET-native equivalents
(`Microsoft.Extensions.AI`, optional Semantic Kernel) are presented in their own
terms.

---

## Decision

### 1. Slice scope = classifier + AI-generated title (one round-trip)

The AI does two jobs in a single structured-output call: classify (`Tags`,
`MatchedSkill`) and produce a short title (`Title`). No summary, no skill-suggestion
queue, no embeddings, no agent loop. Estimated effort 2 days.

### 2. Two interchangeable adapters: Anthropic native + OpenRouter aggregator

Both adapters implement `Microsoft.Extensions.AI.IChatClient`. The operator picks
one at boot via `Ai__Provider=Anthropic|OpenRouter`. The two transports are
deliberately different in shape:

- **Anthropic** uses the native Anthropic API via the `Anthropic.SDK` NuGet
  package's MEAI-compatible `IChatClient` implementation. Vendor-specific features
  available (prompt caching via `cache_control: ephemeral`).
- **OpenRouter** uses `Microsoft.Extensions.AI.OpenAI` against
  `https://openrouter.ai/api/v1`. OpenAI-compatible, so the same package reaches
  hundreds of upstream models (Anthropic, OpenAI, Google, Meta Llama, Mistral,
  Qwen, …) behind one adapter shape.

This earns the *"flexible"* in "intelligente und flexible Services" — one
vendor-native SDK, one aggregator gateway, one shared interface.

### 3. Abstraction = `Microsoft.Extensions.AI` (MEAI), not Semantic Kernel

The Block 3-c Nachbereitung demands "Microsoft.Extensions.AI **oder** Semantic
Kernel als Abstraktion einbinden". MEAI is the right shape for FlowHub's
single-shot classifier-with-typed-output use case: one interface (`IChatClient`),
schema-driven structured output (`CompleteAsync<TResponse>` / `GetResponseAsync`),
decorator chaining (`UseOpenTelemetry`, custom delegates) via `ChatClientBuilder`.
Lightweight; no kernel/plugin/planner machinery.

Semantic Kernel is reserved for a hypothetical Block-5 agent loop (e.g. an
"intelligent retry advisor" that decides between re-route / summarise-and-surface /
escalate when an integration call fails repeatedly) and is explicitly NOT introduced
in Slice C. SK consumes `IChatClient` natively, so adding it later is additive, not
a replacement.

### 4. Port shape = extend `ClassificationResult` with `Title?`

`IClassifier` stays one method. The result record gains an optional third field:

```csharp
public sealed record ClassificationResult(
    IReadOnlyList<string> Tags,
    string MatchedSkill,
    string? Title = null);
```

`KeywordClassifier` returns `Title=null`. `AiClassifier` returns both populated, or
falls back to keyword on AI failure (in which case `Title=null` again). Single
round-trip is meaningfully cheaper than two ports (`IClassifier` + `IEnricher`),
and the model returns both fields naturally inside one schema. Block 4/5 can split
the port if a real use case demands it.

### 5. Failure behaviour = graceful fallback to `KeywordClassifier`

`AiClassifier` wraps `KeywordClassifier` as a hard floor. The fallback triggers on
either:

- **Any exception** thrown by `IChatClient.GetResponseAsync<…>` — network, timeout,
  parse, schema, anything else (catch `Exception`, since MEAI's exact taxonomy
  varies between provider adapters).
- **Defensive post-validation**: even when the call returns successfully,
  `AiClassifier` checks `MatchedSkill ∈ {"Wallabag","Vikunja",""}`. If not, treat
  as a failure (the schema should have prevented this — never trust the model).

In either case the adapter:

1. Logs `Warning` (`EventId 3010 AiClassifierFellBackToKeyword`) with the failure
   reason (exception type or `"schema_violation"`) and elapsed milliseconds.
2. Calls `_keyword.ClassifyAsync(content, ct)` and returns its result with
   `Title=null`.
3. Does NOT rethrow.

Consequences: capture is always classified — AI outage degrades quality, never
availability. The MassTransit retry budget from ADR 0003 §5 stays reserved for
genuine pipeline / bus failures; the fault-observer path (ADR 0003 §6) is not
entered by AI errors.

### 6. Default models: Anthropic Haiku 4.5 / OpenRouter Llama 3.1 70B Instruct

| Provider | Model | Default env | Rationale |
|---|---|---|---|
| Anthropic | `claude-haiku-4-5-20251001` | `Ai__Anthropic__Model` | Cheapest current Claude; strong JSON-schema adherence; tool-use bridge for MEAI. |
| OpenRouter | `meta-llama/llama-3.1-70b-instruct` | `Ai__OpenRouter__Model` | Open-weights; 70B is reliable on schema (smaller variants flake). Sets up the "commercial vs open-weights" rubric narrative. |

Per-call tokens: ~200 input / ~150 output → cost is negligible regardless of
provider; narrative dominates over cost. Both env vars overridable for snapshot
bumps.

### 7. Structured output = `CompleteAsync<T>` with JSON schema generated from the DTO

```csharp
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
    string? Title);
```

MEAI generates the JSON schema from the record. `AllowedValues` translates to a
JSON-schema `enum`; both Anthropic (tool-use under the hood) and OpenRouter
(`response_format: json_schema`) honour it. `MatchedSkill` MUST match a registered
`ISkillIntegration.Name` (`"Wallabag"`, `"Vikunja"`, or `""` → Orphan) or routing
breaks downstream — defensive runtime check inside `AiClassifier` re-validates even
though the schema enforces it.

### 8. No-key startup = silent fallback to `KeywordClassifier`

`AddFlowHubAi(IConfiguration)` inspects `Ai__Provider` and the matching `ApiKey`
and registers `KeywordClassifier` as the active `IClassifier` whenever either is
missing. Invalid `Ai__Provider` values throw `InvalidOperationException` at
startup. `make run` works zero-config for a fresh `git clone` — same dev-friendly
philosophy as `DevAuthHandler` for auth.

Boot logs: `EventId 3020 AiProviderRegistered` (Information) on the AI path,
`EventId 3021 AiProviderNotConfigured` (Information) with reason on the keyword
path. Emitted via a small `IHostedService` (`AiBootLogger`) so the extension stays
a pure DI helper.

### 9. Testing layered: mocked unit tests + trait-gated live integration tests

- 10 mocked unit tests (`tests/FlowHub.Web.ComponentTests/Ai/AiClassifierTests.cs`)
  via `NSubstitute<IChatClient>` and `NSubstitute<IClassifier>` — zero real API
  calls. Cover happy path, argument forwarding (CT, MaxOutputTokens, Temperature),
  the 5 fallback paths (HttpRequestException, TaskCanceledException, JsonException,
  schema violation, generic Exception), and the 3010 logging contract.
- 8 mocked unit tests for the D8 registration matrix
  (`AiServiceCollectionExtensionsTests.cs`).
- 4 trait-gated live integration tests
  (`tests/FlowHub.AI.IntegrationTests/`, `[Trait("Category","AI")]`) that talk to
  real providers — excluded from default `make test`, run via `make test-ai` when
  the operator has keys.

`Makefile` filters: `make test` → `--filter "Category!=AI"`. CI runs `make test`
only; `make test-ai` is operator-on-demand.

### 10. Active provider selection = explicit `Ai__Provider` env var

One env var swaps providers. No implicit precedence; no per-request runtime
selection; no round-robin. Demoing "swap providers via config" is a one-line env
change.

---

## Consequences

### Rubric coverage

This ADR + its implementation directly address six Bewertungskriterien dimensions:

- **Entwurf: Lösungsansatz und Architektur beschrieben** (max 7) — ASCII component
  diagram + data-flow diagrams in the spec.
- **Programmierung: Code lesbar, nach Layer / Modulen strukturiert** (max 7) —
  `FlowHub.AI` becomes a real project with single responsibility; `IClassifier`
  port stays in Core; adapters separated from prompt + DTO.
- **Programmierung: Erkenntnisse aus der Programmierung dokumentiert** (max 3) —
  this ADR + the Slice-C section in `docs/ai-usage.md`.
- **Validierung: Unit-Tests programmiert** (max 3) — 18 mocked unit tests + 4
  trait-gated live integration tests.
- **KI: Wurden KI-Werkzeuge verwendet und deren Nutzung beschrieben** (max 12) ⭐ —
  Slices A/B already covered KI-as-development-tool; this slice adds
  *production-runtime* AI use, which is what the rubric actually asks.
- **KI: Intelligente / flexible Services mit KI gebaut** (max 6) — explicit
  deliverable. Two adapters demonstrate flexibility; graceful keyword fallback
  demonstrates "intelligent" failure handling.

The Quarkus / Jakarta-EE criterion (max 10) remains N/A for the .NET stack.

### EventId namespacing — `3000–3999` reserved for AI

Extends the ADR 0003 §EventId convention:
- `1000–1999` — Pipeline (Slice B)
- `2000–2999` — Skills (Slice B)
- `3000–3999` — AI (this slice)
  - `3001` `AiClassifierStarted` (Debug — Slice C optional, may be omitted)
  - `3002` `AiClassifierSucceeded` (Debug — same)
  - `3010` `AiClassifierFellBackToKeyword` (Warning, on each fallback)
  - `3020` `AiProviderRegistered` (Information, at startup)
  - `3021` `AiProviderNotConfigured` (Information, at startup)

`LoggerMessage` source-gen used throughout (CA1848 / CA1873 enforced by
`Directory.Build.props`).

### Cost guards

`ChatOptions.MaxOutputTokens=300` (~2× the schema's natural ~150 output tokens),
`Temperature=0.2` (deterministic-ish), `HttpClient` 10s timeout.

Anthropic prompt cache: system prompt marked `cache_control: ephemeral` via
`ChatOptions.AdditionalProperties`; ~80% input-token discount on the system-prompt
segment after the second call. OpenRouter prompt cache is not universally
supported across upstream models — Slice C does not claim it. Asymmetry documented
here as a real difference between adapters.

### OpenTelemetry

`Microsoft.Extensions.AI.UseOpenTelemetry()` decorator emits `gen_ai.client.operation
.duration` and token-count metrics on the existing OTEL pipeline. Will surface in
the Block-5 Grafana dashboard alongside MassTransit traces (`AI calls / sec, p95
latency, fallback rate`).

### Reflexion — `Microsoft.Extensions.AI` vs Semantic Kernel

**MEAI** = thin abstraction over chat / embedding / tool-use; sister of
`Microsoft.Extensions.Logging` / `Microsoft.Extensions.Caching`. One interface
(`IChatClient`), schema-driven structured output, decorator chaining. Right pick
when the call site is "give the model a string, get a typed object back".

**Semantic Kernel** = full agent framework: `Kernel`, plugins, planners, memory,
agent runtime. Right pick when an LLM **orchestrates** multi-step work.

Decision rule for FlowHub:

- Single-shot LLM call with typed output → MEAI. Slice C is exactly this.
- Multi-step LLM-driven workflow → add Semantic Kernel without removing MEAI.

A credible Block-5 fit for SK: an *intelligent retry advisor* that, on repeated
integration failures, inspects the failure pattern + capture content + integration
health and decides between re-route / summarise-and-surface / escalate. Slice C
deliberately stops short.

---

## Consequences for next blocks

**Block 4 (Persistence)**:
- `IEmbeddingGenerator<string, Embedding<float>>` already exposed by MEAI →
  embedding work for Block 5 KI-search (pgvector via Npgsql) is incremental, not
  greenfield.
- AI audit fields on the persisted `Capture`: `(provider, model, duration_ms,
  was_fallback)` per classification. Earns the *"Test-Ergebnisse dokumentiert"*
  rubric item with real production data.

**Block 5 (Deployment + KI-search)**:
- ADR 0006 (KI-Suche) builds on the `IEmbeddingGenerator` shape. Anthropic doesn't
  ship embeddings → the embedding provider becomes either OpenAI native, OpenRouter
  passing through, or a self-hosted sentence-transformer behind an OpenAI-compatible
  facade. Asymmetry to document there.
- Integration-test secret rotation: `Ai__Anthropic__ApiKey` /
  `Ai__OpenRouter__ApiKey` move from User Secrets into the Block-5 deployment
  secret store (Authentik or Docker secrets).
- OTEL: `gen_ai.*` metrics export already; Grafana panel for "AI calls / sec, p95
  latency, fallback rate" comes nearly free.
- Optional Semantic-Kernel adoption — additive on top of MEAI.

---

## References

- Brainstorming spec: `docs/superpowers/specs/2026-05-03-slice-c-ai-integration-design.md`
- Implementation plan: `docs/superpowers/plans/2026-05-03-slice-c-ai-integration.md`
- ADR 0001: `docs/adr/0001-frontend-render-mode-and-architecture.md`
- ADR 0002: `docs/adr/0002-service-architecture-and-async-communication.md` — module split
- ADR 0003: `docs/adr/0003-async-pipeline.md` — `IClassifier` port, EventId namespacing
- AI Usage living doc: `docs/ai-usage.md`
- Block 3 Nachbereitung: `vault/Blöcke/03 Service/03 Service - c) Nachbereitung.md`
- Bewertungskriterien: `vault/Organisation/Bewertungskriterien.md`
- `Microsoft.Extensions.AI`: https://learn.microsoft.com/en-us/dotnet/ai/microsoft-extensions-ai
- OpenRouter API reference: https://openrouter.ai/docs

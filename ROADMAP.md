# FlowHub Roadmap

Forward-looking ideas not yet scheduled into a Block. Items here are exploratory — promote to an ADR + implementation plan before building.

---

## Capture Enrichment (post-classification data fetch)

**Status:** Idea — not scoped into any Block.
**Motivation:** Today the classification consumer (`CaptureEnrichmentConsumer`) only *labels* a Capture — tags, matched skill, AI-generated title. All three are derived from the Capture's own text. There is no step that **fetches additional data** based on the classified content. Example: a Capture containing a quote *"The unexamined life is not worth living." — Socrates* gets classified, but no biographical info about Socrates is attached.

### Proposed shape

1. **New port in Core:** `IEnricher.EnrichAsync(Capture, ct)` returning structured extras (e.g. `AuthorBio`, `SourceUrl`, `RelatedQuotes`).
2. **New consumer:** subscribes to `CaptureClassified` (runs *after* classification, so enrichment only fires for known types — not on every Capture).
3. **Persistence:** sibling `CaptureEnrichment` table (keeps the core Capture untouched; enrichment failures don't corrupt the original).
4. **New event:** `CaptureEnriched` so the UI and search re-index can react.

### Implementation options

- **Tool use via MEAI** — `ChatOptions.Tools` with `AIFunction`s wrapping Wikidata / Wikipedia / Brave / Tavily. `FunctionInvokingChatClient` decorator handles the call/respond loop automatically.
- **Plain HTTP adapter** (no LLM) when the source has a clean API — cheaper and deterministic.
- **Semantic Kernel agent loop** — only if enrichment needs multi-step reasoning ("try Wikidata → fall back to Wikipedia → fall back to web search → reconcile conflicts"). ADR 0004 §Reflexion already reserves SK for exactly this kind of workflow; it consumes `IChatClient` natively, so adding it is additive on top of MEAI.

---

## Web Search Strategy

If/when enrichment needs open-web data, three paths exist:

### 1. Self-provided web-search tool (recommended)

Wrap Brave Search / Tavily / DuckDuckGo behind an `AIFunction`. Works **today** on MEAI across both adapters (Anthropic + OpenRouter) — tool use is part of the `IChatClient` contract.

```csharp
var lookupAuthor = AIFunctionFactory.Create(
    (string name, CancellationToken ct) => _wikidata.GetAuthorBioAsync(name, ct),
    name: "lookup_author",
    description: "Look up biographical info for a person by name.");

var chatClient = baseChatClient
    .AsBuilder()
    .UseFunctionInvocation()
    .Build();

await chatClient.GetResponseAsync<EnrichmentResponse>(
    prompt,
    new ChatOptions { Tools = [lookupAuthor] },
    ct);
```

**Trade-offs:**

- `+` Portable across providers — no `Ai__Provider`-conditional code.
- `+` Deterministic — Wikidata/Wikipedia give clean structured data; web search returns noisy HTML.
- `+` Cheap at FlowHub volume — Wikidata is free; Brave/Tavily have generous free tiers.
- `−` You write the adapter (one HTTP client + DTOs). Half a day, not a week.
- `−` No open-web capability unless you also wrap Tavily/Brave as a second tool.

### 2. Provider-hosted web search

Anthropic `web_search_20250305` and OpenAI hosted `web_search` run **inside the provider**. Cheaper to wire (no Brave/Tavily key) but **not part of the MEAI `IChatClient` contract** — vendor-specific server tools reached via `ChatOptions.AdditionalProperties` or the native client. OpenRouter passthrough is inconsistent across upstream models. Same asymmetry pattern as ADR 0004's note on Anthropic prompt caching.

### 3. OpenRouter `:online` model variants

Appending `:online` to a model slug (e.g. `meta-llama/llama-3.1-70b-instruct:online`) attaches an automatic web-search pre-step server-side. Zero code change beyond the model env var. Cheapest demo path; least control over what gets searched.

### Recommendation

Default to **option 1** for the same reason ADR 0004 picked MEAI over Semantic Kernel: it stays inside the abstraction and keeps both providers symmetric. Reserve options 2/3 for demo scenarios where speed-to-prototype matters more than determinism.

---

## Additional AI Providers (Gemma, Apertus, Hugging Face)

**Status:** Idea — extension of ADR 0004.
**Motivation:** Today FlowHub ships two adapters (Anthropic native, OpenRouter aggregator). Adding more provider classes — particularly a **Swiss sovereign open-weights** model — strengthens the *"intelligente und flexible Services"* and *"KI-Werkzeuge verwendet"* rubric narrative beyond what two commercial providers can show.

### Candidates

#### Google Gemma — easy, env-var change

Three reachable paths via the **existing OpenAI-compatible adapter**:

1. **OpenRouter passthrough** — pure env change: `Ai__OpenRouter__Model=google/gemma-3-27b-it`. Zero code.
2. **Google AI Studio / Vertex AI directly** — both expose OpenAI-compatible endpoints. Needs the configurable `BaseUrl` refactor below.
3. **Self-host (Ollama / vLLM / llama.cpp)** — `BaseUrl=http://ollama:11434/v1`, `Model=gemma3:27b`. Air-gapped, zero cloud cost.

**Caveat:** structured-output adherence varies by size. 27B variants honour `response_format: json_schema` well; 4B/9B variants flake — same warning ADR 0004 §6 records for smaller Llamas. `KeywordClassifier` fallback is the safety net.

#### Apertus (Swiss ETH/EPFL/CSCS open-weights model) — possible, more wiring

1. **Public AI Inference Utility / Swisscom** — OpenAI-compatible endpoint. Smallest code change.
2. **Hugging Face Inference Providers / Router** — `https://router.huggingface.co/v1` is OpenAI-compatible; same adapter shape as OpenRouter, just a different aggregator. Availability of Apertus on serverless depends on which HF providers carry it; dedicated Inference Endpoints always work but cost a reserved GPU.
3. **Self-host on a GPU box** — weights on Hugging Face under an open licence. Serve via vLLM / Ollama → OpenAI-compatible endpoint. Realistic for a homelab demo.

**Verify before committing:** Apertus's structured-output / function-calling maturity. Newer open-weights models often lag here. If strict JSON emission is unreliable, the options are grammar-constrained decoding (vLLM `guided_json`) or accepting higher fallback rates.

#### Hugging Face Router as a generic option

Worth treating as a third aggregator alongside OpenRouter in its own right — reaches many open-weights models (Llama, Mistral, Qwen, Gemma, Apertus when available, …) via one OpenAI-compatible base URL. Same `Ai__OpenAi__BaseUrl` refactor unlocks it.

### Required refactor

Small, contained in `source/FlowHub.AI/AiServiceCollectionExtensions.cs`:

1. **Configurable `BaseUrl`** on the OpenAI-compatible path (currently hardcoded to OpenRouter). Add `Ai__OpenAi__BaseUrl` / `Ai__OpenAi__ApiKey` / `Ai__OpenAi__Model`, and an `AiProvider.OpenAiCompatible` enum value.
2. **Document per-model asymmetries** as an extension of ADR 0004 §6 — Gemma 27B "fine for schema"; Gemma 4B "expect fallbacks"; Apertus "verify schema support per release".

### Embeddings unchanged

Neither Gemma nor Apertus ships a mature dedicated embedding model. Embeddings stay on Mistral per ADR 0006 — chat and embeddings remain provider-asymmetric (already documented there).

### Rubric angle

Three vendor classes — commercial native (Anthropic), commercial aggregator (OpenRouter), Swiss sovereign open-weights (Apertus) — demonstrate *"flexible"* far more convincingly than two commercial endpoints. A small follow-up ADR can record this as an extension point even if the runtime default stays Anthropic + OpenRouter for the CAS submission.

---

## References

- ADR 0004 — AI Integration in Services (`docs/adr/0004-ai-integration-in-services.md`)
- ADR 0006 — Vector Search (`docs/adr/0006-vector-search.md`)
- `source/FlowHub.Web/Pipeline/CaptureEnrichmentConsumer.cs` — current classify-only "enrichment" consumer

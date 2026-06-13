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

## `USER.md` — human's personal context for skill generation

**Status:** Idea — not scoped into any Block.
**Motivation:** Skills today are generated without any durable model of *who the user is*. A `USER.md` at the repo root would hold the human's personal context so newly generated skills can be tailored to their background, stack, and life — instead of being generic.

### Proposed shape

A short, hand-curated Markdown file capturing stable personal facts, e.g.:

- **Role:** Software Engineer
- **Tech stack:** .NET / C#
- **Hobbies:** Homelab, …
- **Family:** children, …

### How it's used

`USER.md` is **read as context when generating new Skills**, so the generator can:

- Pick examples and defaults that match the user's stack (.NET/C# over, say, Python).
- Propose skills that fit the user's actual life (homelab automation, family logistics) rather than generic templates.
- Skip onboarding questions whose answers already live in `USER.md`.

### Open questions

- Location & format — repo root `USER.md` vs. a vault note vs. `.ai/` config.
- Overlap with the existing memory store (`memory/MEMORY.md`) — `USER.md` is hand-curated and skill-generation-facing; memory is auto-accumulated session context. Decide whether they cross-reference or stay separate.
- Privacy — keep personal/family details out of anything that ships in the CAS submission bundle.

---

## Marketplace for Skills

**Status:** Idea — not scoped into any Block.
**Motivation:** Skills (`ISkillIntegration` adapters like Wallabag and Vikunja) are hard-wired into the app today — adding a new target service means writing code, registering DI, and shipping a release. A marketplace would let a Skill be **discovered, installed, and configured at runtime** without touching the core, turning FlowHub from a fixed set of integrations into an extensible platform.

### Proposed shape

1. **Skill manifest** — each Skill ships a descriptor (name, icon, capabilities, required config keys, the Capture types it handles) so it can be listed and configured without code knowledge.
2. **Registry** — a catalogue the app reads from. Start with a curated in-repo list; later a remote index (community-contributed Skills) with versioning + checksums.
3. **Install/enable flow in the UI** — browse available Skills, supply the per-Skill secrets (endpoints, tokens), enable/disable per user. Reuses the existing `Skills__*` config surface.
4. **Isolation & trust** — sandbox third-party Skills (out-of-process or capability-scoped) so a bad Skill can't read unrelated Captures or exfiltrate other Skills' credentials.

### Open questions

- Distribution unit — in-process plugin assemblies vs. out-of-process services (MCP-style) the app talks to over a contract.
- Trust model — signing, review, and what a Skill is allowed to touch; how secrets are scoped per Skill.
- Overlap with the CC-skills layer (`flowhub-capture`, `flowhub-triage`, …) — decide whether the marketplace covers integration adapters only, or also the agent-facing skills.
- Monetisation / licensing if community Skills are ever sold (ties into the post-CAS product spinout).

---

## Speech-to-Text for Captures (voice notes)

**Status:** Idea — not scoped into any Block.
**Motivation:** Captures today are text or URLs. A voice message — e.g. a Telegram voice note or a Messenger audio clip — can't be captured. Speech-to-text would transcribe an incoming audio message into a Capture's body so it flows through the normal classification + routing pipeline like any other Capture.

### Proposed shape

1. **Channel accepts audio** — the capture channel (Telegram first, other messengers later) detects an audio/voice payload instead of text.
2. **Transcribe** — run the audio through an STT engine; the transcript becomes the Capture body.
3. **Normal pipeline** — the transcript is classified, tagged, and routed exactly like a typed Capture; optionally keep a reference to the original audio.

### Open questions

- STT engine — local (`whisper.cpp` / faster-whisper) for €0 + privacy vs. a hosted API for accuracy/latency.
- Language detection (DE/EN mix) and whether to store or discard the original audio.
- Cost + size limits on inbound audio per channel.

---

## Forge Routing — Skill decides "idea" vs. "issue"

**Status:** Idea — not scoped into any Block.
**Motivation:** A Capture like *"flowhub: Add i18n"* references a software project. Today `flowhub-issue` always creates an issue. But not every such Capture is actionable — some are vague ideas that belong on a backlog, not in a forge tracker. The Skill should **decide intent** and route accordingly.

### Proposed shape

1. **Intent classification** — the FlowHub Skill judges whether the Capture is an actionable task or a loose idea.
2. **Actionable + repo identifiable** → create a forge issue (reuses the existing `flowhub-issue` repo-resolution across forges).
3. **Vague idea** → append to an ideas/backlog target (e.g. the project's `ROADMAP.md` or a Vikunja "Ideen" list) instead of opening an issue.

### Open questions

- Where ideas land — repo `ROADMAP.md`, a dedicated vault note, or a Vikunja project — and how the user confirms/overrides the decision.
- Confidence threshold before auto-creating an issue vs. asking the user first.
- Builds on the existing `flowhub-issue` skill (forge detection + repo resolution).

---

## "Now Playing on SRF 3" Skill (radio track lookup)

**Status:** Idea — not scoped into any Block.
**Motivation:** You hear a great track on SRF 3 and want to remember it — but by the time you reach for your phone the song is over and you never caught the title. A Skill that fetches the **currently playing track** turns a fleeting moment into a Capture: artist + title, captured and routed to a music list with one trigger.

### Proposed shape

1. **Data source** — SRF 3's now-playing / "Musikliste" feed (artist, title, airtime).
2. **Trigger** — a Capture like *"srf3 now playing"* (or a quick-action button) fires the Skill instead of routing the text literally.
3. **Fetch + capture** — the Skill resolves the current track and creates a Capture (`<artist> – <title>`), routed to a music target (e.g. a Vikunja "Musik" list, or later a playlist service).

### Open questions

- Data source — does SRF expose an official/stable now-playing API, or must the playlist page be scraped? (rate limits + ToS).
- Target — where captured songs land (Vikunja list, ListenBrainz/Spotify playlist, plain Capture).
- "Now" vs. "last N tracks" — a small history makes it forgiving if you trigger slightly late.
- Generalise to other stations (SRF 1/2, other radios) → a natural fit for the [Marketplace for Skills](#marketplace-for-skills) idea above.

---

## LLM Performance Benchmarking (LLMeter)

**Status:** Idea — not scoped into any Block.
**Motivation:** Observability today captures *real-world* LLM latency and token usage via OpenTelemetry, but there's no *controlled* characterisation of how the classifier behaves under varying load — how latency scales with Capture size, where concurrency starts to degrade, tokens-per-second per provider/model. That data is what turns "we monitor the LLM" into "we measured and **optimised** it" (the optimisation half of the Block 5 Monitoring Lernziel).

### Proposed shape

1. **Tool** — [LLMeter](https://github.com/awslabs/llmeter), a lightweight LLM-endpoint benchmarking library (latency / throughput / tokens-per-sec across payload sizes and concurrency, with plots). OpenRouter is OpenAI-compatible, so it can target FlowHub's configured provider directly.
2. **A one-off study, not infra** — a small runnable script + a written result in `docs/insights/` (latency-vs-Capture-size curve, concurrency ceiling, model comparison). Not added to the app or compose.
3. **Feeds tuning** — results inform classifier timeout, retry/fallback thresholds, and model choice.

### Open questions

- **Cost guard** — the demo's OpenRouter key has a **$1 hard cap**; benchmark runs must be small and rate-limited, or pointed at a separate paid key.
- Overlap with the existing OTel metrics — LLMeter adds controlled benchmarking on top of live telemetry; decide how much is worth the spend.
- Whether to keep it manual or wire a periodic CI benchmark (probably manual — cost).

---

## References

- ADR 0004 — AI Integration in Services (`docs/adr/0004-ai-integration-in-services.md`)
- ADR 0006 — Vector Search (`docs/adr/0006-vector-search.md`)
- `source/FlowHub.Web/Pipeline/CaptureEnrichmentConsumer.cs` — current classify-only "enrichment" consumer

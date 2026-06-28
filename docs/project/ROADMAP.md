# FlowHub Roadmap

Forward-looking ideas not yet scheduled into a Block. Items here are exploratory — promote to an ADR + implementation plan before building.

> **Why this list is mostly cheap to build.** FlowHub is a hexagonal modular
> monolith: capture sources are **driving adapters** behind one entry point
> (`ICaptureService`), and downstream targets are **driven adapters** behind one
> port (`ISkillIntegration`); classification sits behind `IClassifier`, the LLM
> behind a provider abstraction, and the pipeline behind MassTransit (transport
> swappable in-memory↔RabbitMQ). So **a new channel, a new skill target, or a new
> AI provider is a new adapter — not a core change.** The items below are grouped
> by which seam they extend, which is the real point: FlowHub is built to grow.

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

## LiteLLM Proxy — provider gateway in front of the AI abstraction

**Status:** Idea / open question — the infrastructure counterpart to [Additional AI Providers](#additional-ai-providers-gemma-apertus-hugging-face). Post-CAS (adds a non-.NET runtime dependency; out of the frozen submission scope).
**Question on the table:** *Is it worth putting a [LiteLLM Proxy](https://github.com/BerriAI/litellm) between FlowHub and the LLM providers?*

LiteLLM Proxy is an open-source gateway that exposes **one OpenAI-compatible endpoint** in front of 100+ providers (Anthropic, OpenAI, OpenRouter, Mistral, Bedrock/Vertex, Ollama, Hugging Face, …) and adds virtual keys, per-key **budgets + rate limits**, spend tracking, model-level **fallback / load-balancing / retries**, response caching, guardrails, and logging callbacks (Langfuse / OTel / Prometheus).

### Why it's a near-zero-code fit

FlowHub already abstracts providers in-process behind MEAI `IChatClient`, and the OpenAI-compatible adapter needs only a base URL + key + model. So pointing FlowHub at a proxy is exactly the **configurable `BaseUrl` refactor** the *Additional AI Providers* item already calls for — `Ai__OpenAi__BaseUrl=http://litellm:4000/v1` and done. No per-provider C# adapters.

### What it would buy us (mapped to existing roadmap/Demo concerns)

- **Subsumes most of *Additional AI Providers*** — adding Gemma / Apertus / an HF model becomes a **LiteLLM config edit**, not a FlowHub adapter + `AiProvider` enum value. The app stops growing provider classes.
- **Hard cost caps** — the Demo's manual **$1 OpenRouter cap** (see `docs/project/DEMO.md`) and the [LLMeter](#llm-performance-benchmarking-llmeter) cost-guard become a virtual-key budget enforced at the gateway, not by hand.
- **Gateway-level fallback / load-balancing** — Gemma → Llama failover without touching FlowHub; the deterministic `KeywordClassifier` stays the final in-app safety net (defence in depth, not replaced).
- **Central spend + usage logging** — one place for `gen_ai.*` cost/usage, complementing (or partly overlapping) the in-app OpenTelemetry metrics from ADR 0004 — decide gateway-owns vs. app-owns.
- **Prompt-level visibility for debugging** *(arguably the strongest single-operator pull)* — LiteLLM's request logs + Admin UI (and Langfuse / OTel callbacks) capture the **exact rendered system prompt, the raw Capture, the structured-output schema, and the model's raw JSON response** per call. That's precisely what FlowHub can't see today: the in-app `gen_ai.*` metrics record **token counts + latency, not message content** (ADR 0004 / `ClassifierTrace`), and the system prompt is built dynamically (the live bucket list is interpolated into `AiPrompts.BuildSystemPrompt`). Being able to replay *"what actually went to the model"* turns the `KeywordClassifier` fallbacks (log EventId 3010 — schema violation? what did the model emit?) and misclassifications into something **inspectable instead of guesswork**.

### Why it's *not* obviously worth it

- **A non-.NET runtime dependency** — LiteLLM is a Python/FastAPI service with its own Postgres (keys/spend). That's real operational weight against the lean **€4.50/mo single-operator** posture; the demo deliberately runs *fewer* moving parts.
- **A second abstraction doing a similar job** — MEAI `IChatClient` already routes providers in-process. LiteLLM is a *second* indirection at the network boundary; the split must be deliberate (MEAI owns the typed call + structured output; LiteLLM owns routing / keys / budgets) or it's just layering.
- **Structured-output + native-feature passthrough** — FlowHub leans on JSON-schema structured output (`GetResponseAsync<T>`) and some **Anthropic-native** behaviour (prompt caching, ADR 0004). Routing Anthropic through an OpenAI-compatible shape can drop vendor-specific features and must be verified to forward `response_format`/`json_schema` faithfully per upstream model — the same per-model asymmetry the *Additional AI Providers* and *Web Search Strategy* items already flag.
- **Prompt logging *is* capture-content logging** — the flip side of the debugging win: seeing the input means the gateway now **stores Capture content** (and ships it to whatever Langfuse / OTel sink the callback targets). That collides head-on with the privacy NFR (**NfA-P1**, local-by-default — content shouldn't leave the homelab; ADR 0007 / [Local LLM via Ollama](#local-llm-via-ollama--full-data-residency-nfa-p1)) and with the demo's ephemeral-by-design 15-min reset, where logs could persist visitor-submitted text past the window. Acceptable only as **dev-profile or a local-only sink with redaction** — never on by default in prod/demo.

### Recommendation

**Defer, but record it as the preferred path *if* multi-provider + cost-governance ever becomes a real need** (e.g. the post-CAS product spinout serving multiple users/keys). For the single-operator homelab and the CAS demo, the in-app MEAI abstraction + `KeywordClassifier` fallback already covers provider-swap and resilience at zero extra infrastructure — a gateway is justified by **fleet/billing/governance** needs FlowHub doesn't have yet. When it does, LiteLLM largely replaces the *Additional AI Providers* refactor rather than competing with it.

The one pull that's tempting **even today** is the prompt/response-level debugging — but if that's the only driver, it's lighter to reach it *without* a gateway: either flip on the OTel GenAI **message-content capture** (opt-in event logging, kept on a local-only sink) inside FlowHub's existing telemetry, or wire a small dev-profile `IChatClient` logging decorator. Adopt LiteLLM when you want *several* of {multi-provider, budgets, fallback, central spend, prompt logs} at once — not for prompt visibility alone. Either way, a short follow-up ADR should make the gateway-vs-in-app boundary (and the NfA-P1 content-logging line) explicit before adopting.

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

## Declarative Skills + declarative target interaction (no-rebuild authoring)

**Status:** Idea — not scoped into any Block. Foundation the *Marketplace for Skills* (above) would build on.
**Motivation:** Today a Skill is a compiled `ISkillIntegration` adapter — adding a new skill *or* a new target service means writing C#, registering DI, and shipping a release. The concept-phase pitched a declarative `SKILL.md` hybrid (see Projektbeschreibung v4 §6.2 / ADR 0002); it was descoped because the **target-system communication itself** (HTTP path, auth, payload mapping, response parsing) lives in the adapter and resisted declaration. The boost is unlocked only if **both halves** go declarative: the Skill *and* its target interaction. Then introducing a skill/target is dropping in files, not recompiling.

### Proposed shape

1. **Declarative Skill descriptor** — a `SKILL.md` with YAML frontmatter authored like a Claude Skill (name, description, trigger keywords / URL patterns, required config keys, which target it routes to). The `SkillRegistry` discovers and loads these at runtime instead of resolving compiled types by name.
2. **Declarative target interaction** *(the hard part — brainstorm)* — a descriptor for the outbound call: endpoint + method, auth scheme, a mapping from `Capture` fields → request body, and a path from response → external ref. Candidate mechanisms to weigh:
   - a request-template / mapping DSL (e.g. JSONata / Liquid / Scriban) over a constrained schema;
   - an **MCP-style contract** where the target is an external service FlowHub talks to over a stable protocol (sidesteps bespoke HTTP entirely);
   - a generated typed client from an OpenAPI/descriptor (Refit-style) loaded dynamically.
3. **Hybrid escape hatch** — declarative-first, but a target too bespoke for the DSL (Wallabag's OAuth refresh, Paperless' multipart upload, Vikunja's project-id resolution) can still fall back to a compiled `ISkillIntegration`. Declarative covers the common case; code covers the long tail.
4. **Payoff** — a new skill/target = author two declarative files (or one `SKILL.md` referencing a target profile), enable in the UI, done. No rebuild, no redeploy — the customization story the concept phase wanted.

### Open questions

- **Expressiveness vs. safety of the mapping DSL** — how much request/response shaping can be declared before it becomes a programming language; where the compiled-adapter fallback line sits.
- **Auth declaratively** — Bearer / API-key / OAuth-refresh expressed without leaking secrets; reuse the existing `Skills__*` config surface for the actual credentials.
- **Trust / SSRF** — a declarative skill that can name any endpoint is an exfiltration risk; needs allow-listing + the Marketplace's sandboxing/trust model.
- **Validation & testing** — contract-test a declarative skill (e.g. WireMock) *without* a rebuild, so authoring stays fast.
- **Format reuse** — align the `SKILL.md` frontmatter with the CC-skills convention (`flowhub-capture`, `flowhub-triage`, …) so one mental model covers both layers.

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

## Ausflug-Assistant — natural-language recommender over a Vikunja "Ausflugsziele" project

**Status:** Idea — not scoped into any Block. The first *read-side* use case (everything else on this list is capture → route; this is query → recommend → write-back).
**Motivation:** A Vikunja project **`Ausflugsziele`** accumulates excursion destinations over time (Ausflüge mit Tochter, Freundin, Wandergruppe). Today it's a flat todo list. The value is locked up until you can *ask* it:

> *"Kommenden Sonntag (heute ist 19.06.2026) möchte ich mit meiner Tochter einen Ausflug machen. Nicht zu weit entfernt mit dem Auto. Soll draussen sein, aber nicht die ganze Zeit an der Sonne. Wir haben 3 Stunden Zeit."*

The assistant parses that, filters/ranks the catalogue against the constraints, proposes one option, and — once visited — marks it done with a `DoneDate`. This inverts the existing pipeline: instead of a Capture flowing *in* and being routed, a query flows in and a stored destination flows *out* as a recommendation.

### Two genuinely new pieces (everything else composes existing seams)

1. **A read/query surface** — request/response, user-initiated (chat, a Telegram command, a quick-action), distinct from the capture channels which are fire-and-forget inbound.
2. **A visit-log / revisit data model** — see below; the one part that doesn't already exist.

Everything else is reuse: the Vikunja adapter, the AI provider abstraction, pgvector ([Semantic features on the existing pgvector index](#semantic-features-on-the-existing-pgvector-index) / ADR 0006), the enrichment seam ([Capture Enrichment](#capture-enrichment-post-classification-data-fetch)), the tool-use pattern ([Web Search Strategy](#web-search-strategy)), and the confirm step ([Confidence-driven human-in-the-loop](#confidence-driven-human-in-the-loop)).

### Proposed shape

1. **Enrich each destination on capture** — when an Ausflug enters the project, derive structured attributes so it's queryable later: drive-time-from-home (geocode + routing), indoor/outdoor/mixed, shade vs. open-sun, typical duration, suitable-for (Kind / Freundin / Wandergruppe), season/weather suitability. This *is* the [Capture Enrichment](#capture-enrichment-post-classification-data-fetch) seam, applied to a new type.
2. **Parse the query into a structured filter** — an LLM (the existing structured-output pattern, `AiClassificationResponse`-style) turns the free-text request into typed constraints: `maxDriveMinutes`, `setting=outdoor`, `shade=partial`, `durationHours≈3`, `companion=child`, `date=2026-06-21`.
3. **Hybrid retrieval + rank** *(the core decision — see brainstorm)*:
   - **deterministic filter** for the hard, numeric constraints (drive-time, duration, indoor/outdoor) — embeddings can't reason about "3 Stunden" or "nicht zu weit";
   - **pgvector semantic match** for the soft/vibe part ("etwas Ruhiges in der Natur");
   - a small scoring function combines them + a **recency/novelty** term (see revisit model).
4. **Weather-aware for the specific date** — "nicht die ganze Zeit an der Sonne" + a concrete Sunday ⇒ call a forecast API for that date/region and factor it in (rain → prefer indoor fallback; blazing sun → prefer forest/shade). A tool call, not vector search — the standout demo hook.
5. **Propose → confirm → write back** — surface one (or top-3) options with a one-line *why*; on confirmation, record the visit (`DoneDate`).

### The DoneDate / revisit problem (the real modelling question)

A Vikunja task has a single `done` boolean + one `done_at`. That's a one-shot model and it breaks for places like **Zoo Basel ("Zolli")** that are visited many times and stay valid candidates *after* a visit.

- **Separate the catalogue from the visit history.** The Vikunja project is the *catalogue of destinations*; visits are an **append-only log** (`ExcursionVisit { destinationRef, visitedOn, companion }`) owned by FlowHub. A revisitable place is **never "closed"** — it accumulates *n* visit dates.
- **`revisitable: bool` per destination.** A one-shot idea (a specific event) marks done normally and drops out; a revisitable place uses the visit log instead and its score **decays with recency** ("you were at the Zolli two weeks ago — maybe somewhere new") rather than being filtered out.
- **Where the log lives** — open question: FlowHub's own DB (clean, but splits state across two systems) vs. dated Vikunja **comments** on a perpetually-open task (stays in Vikunja, hacky) vs. Vikunja **recurring tasks** (calendar-recurrence ≠ "visited on these dates" — semantic mismatch, probably wrong).

### In FlowHub or outside?

- **Prototype as a CC-skill / agent first** (`flowhub-ausflug`, sibling to `flowhub-triage`) — reads Vikunja, calls weather + drive-time tools, proposes. Zero app change, fastest path, fits the existing skills layer.
- **But the durable pieces belong in the app:** the per-destination enrichment attributes and the visit-log / revisit model. Same split the repo already uses (app pipeline vs. CC-skills). The conversational assistant sits on top of an enriched, queryable catalogue.

### Open questions

- **Drive-time source** — a routing API (OpenRouteService / Google) vs. a one-time enrichment that stores drive-time-from-home per destination (cheaper at query time, stale if "home" changes).
- **Vector vs. structured weight** — how much ranking is hard-filter vs. semantic vs. weather vs. recency; keep it explainable (the user should see *why* the Zolli won).
- **Cold start** — destinations captured before enrichment existed have no attributes; backfill enrichment pass vs. enrich-on-first-query.
- **Multi-modal** — a Wandergruppe vs. a 5-year-old are different constraint profiles on the same catalogue; `companion` as a first-class filter dimension.
- **Discovery beyond the list** — when nothing fits, should it *web-search* a new idea and offer to capture it (ties into [Web Search Strategy](#web-search-strategy))?

**Architecture payoff:** demonstrates the same ports run *backwards* — the Vikunja adapter as a read source, the AI abstraction for query-parsing + ranking, pgvector for semantic match, the tool-use seam for weather/routing — proving FlowHub is a substrate, not just an inbound funnel. Only the visit-log model and the query surface are net-new.

---

# Extensibility showcase — adapters the ports make cheap

## paperless-ngx integration — documents & Belege (incl. its own AI)

**Status:** Idea — not scoped into any Block.
**Motivation:** FlowHub already routes links to Wallabag and tasks to Vikunja. The natural next homelab target is **paperless-ngx** for document/receipt/Beleg captures (PDFs, scans, photos of paperwork) — closing the "everything in my inbox finds its home" vision.

### Proposed shape

1. **New driven adapter** `PaperlessSkillIntegration : ISkillIntegration` — uploads the capture's attachment to the paperless-ngx REST API (`/api/documents/post_document/`), returns the document id as `ExternalRef`. Same pattern as Wallabag/Vikunja.
2. **AI-to-AI handoff:** paperless-ngx runs its **own** OCR + (in recent versions) LLM-based title/tag/correspondent inference. FlowHub does the *routing* decision; paperless-ngx does the *document* enrichment. Optionally feed paperless-ngx's extracted text back as a follow-up Capture for cross-linking.
3. **Attachment path:** the `Capture.Attachment` + `IAttachmentStorage` plumbing already exists for binary captures.

**Architecture payoff:** one new `ISkillIntegration` class + options + DI line. Zero domain or pipeline change — this is purely a homelab-side adapter.

---

## More capture channels — Telegram, share-target, email

**Status:** Idea (Telegram is a reserved placeholder).
**Motivation:** "Capture without friction" means capturing from wherever you already are. The Web Quick-Capture + REST API are two channels; the architecture is built for more.

### Proposed shape

- **Telegram bot** — webhook → `ICaptureService.SubmitAsync(..., ChannelKind.Telegram)`. The original one-message-capture vision; realizes the `FlowHub.Telegram` placeholder.
- **OS / PWA share-target** — register FlowHub as a share target so any app's "Share" sheet can send to it.
- **Email-to-capture** — forward an email to a dedicated address → capture. Full treatment in [Email-to-Capture channel](#email-to-capture-channel) below.

**Architecture payoff:** each is a **driving adapter** in front of the existing `ICaptureService` — no new domain, classification, or routing code. The `ChannelKind` enum + per-channel adapter is the only surface that grows.

---

## Email-to-Capture channel

**Status:** Idea — the email leg of [More capture channels](#more-capture-channels--telegram-share-target-email), broken out because email carries specifics the others don't (spoofable sender, attachments, forwarded-mail chrome).
**Motivation:** Email is the universal "send it somewhere" channel — every device has it, and "forward this to capture it" is zero-friction. Forward a newsletter, an order confirmation, a receipt PDF, or a *"remember this"* note to a dedicated address and it becomes a Capture that flows through the normal classify → route pipeline.

### Proposed shape

1. **New driving adapter** in front of `ICaptureService` — `ICaptureService.SubmitAsync(..., ChannelKind.Email)` (new `ChannelKind.Email`). No domain/classification/routing change; same seam as the Telegram/REST channels.
2. **Two ingestion modes** (pick per deployment):
   - **IMAP poll** — a background worker polls a dedicated mailbox (e.g. a homelab catch-all or a Gmail app-password account) on an interval, ingests unseen messages, marks them read. Simplest, works with any mailbox, no inbound DNS/MX. Fits the self-hosted posture.
   - **Inbound webhook** — a mail provider (Mailgun / Postmark / SendGrid inbound-parse) or a self-hosted MX forwards parsed mail to an HTTP endpoint. Lower latency, no polling, but adds a provider + a public endpoint to secure.
3. **Message → Capture mapping** — subject + a cleaned body become the Capture content; sender/Message-ID retained as metadata. **Forwarded-mail unwrapping**: strip the `Fwd:`/quoted-reply chrome and signatures so the *original* URL/text is what gets classified, not the forwarding envelope.
4. **Attachments** — emails carry files; reuse the existing `Capture.Attachment` + `IAttachmentStorage` plumbing (already built for the paperless-ngx path). Enforce the same size cap + content-type allowlist at the boundary; a PDF receipt naturally routes onward to paperless.

### Open questions

- **Anti-spoofing / trust** *(the email-specific one)* — an email's `From` is trivially forged, so a publicly-known address is an open injection vector. Options: an **unguessable plus-token** address (`capture+<secret>@…`), a **sender allowlist**, and/or verifying **SPF/DKIM/DMARC** on inbound. Decide before exposing any address. Ties into [Confidence-driven human-in-the-loop](#confidence-driven-human-in-the-loop) for low-trust senders.
- **Mailbox/domain** — dedicated homelab domain catch-all vs. a hosted inbox; who owns MX.
- **Body extraction** — HTML→text, signature/disclaimer stripping, and which part wins on multipart `text/plain` + `text/html`.
- **Loop & dedup safety** — idempotency by `Message-ID`; never auto-reply (avoid bounce/auto-responder loops).
- **Privacy** — inbound mail content leaves to the LLM classifier like any Capture; same NfA-P1 consideration as elsewhere.

**Architecture payoff:** one new driving adapter + `ChannelKind.Email`; the IMAP-poll variant needs no public endpoint at all. Attachment handling and routing already exist — this is additive, not a core change.

---

## GitHub-Issue Skill — a Capture becomes a forge issue

**Status:** Idea — partially prototyped as the `flowhub-issue` CC-skill; see also *Forge Routing* above.
**Motivation:** Dogfooding: a Capture like **`flowhub: Add i18n DE/EN`** should open a real GitHub issue in this very repo — title *"Add i18n (DE/EN)"*, body with the captured context, labels inferred from the text. FlowHub managing its own backlog is a compelling live demo for the examiner.

### Proposed shape

1. **New driven adapter** `GitHubIssueSkillIntegration : ISkillIntegration` — resolves the target repo from the capture prefix (`flowhub:` → `freaxnx01/FlowHub-CAS-AISE`) and POSTs to the GitHub Issues API; `ExternalRef` = issue number/URL.
2. **Classifier routes** software-project captures to this skill (matched skill `GitHubIssue`), reusing the multi-forge repo-resolution already in the `flowhub-issue` skill.
3. **Idea vs. issue** — defer the vague-vs-actionable decision to the *Forge Routing* item above (idea → backlog, actionable → issue).

**Architecture payoff:** same `ISkillIntegration` shape as Wallabag/Vikunja; the forge-detection logic already exists in the `flowhub-issue` skill, so this is mostly wiring it as an in-app skill target. *(The `Add i18n DE/EN` example is itself a genuine FlowHub enhancement — a fitting first dogfooded issue.)*

---

## Further homelab skill targets

**Status:** Idea.
**Motivation:** The `ISkillIntegration` port is the extension point for the whole self-hosted ecosystem.

- **Karakeep / Hoarder** — bookmarks with AI tagging (alternative/complement to Wallabag).
- **Immich** — photo captures → albums.
- **Firefly III / Actual** — receipt/expense captures → finance entries.

**Architecture payoff:** each is a thin adapter; the matched-skill registry resolves by name, so adding a target is additive.

---

## Agentic, multi-step classification

**Status:** Idea — `IClassifier` abstraction already isolates this; Semantic Kernel reserved (ADR 0004).
**Motivation:** Today classification is single-shot. Some captures need reasoning + tools — *"fetch this opaque share URL, read the page title, then decide the skill."*

### Proposed shape

- A tool-using agent loop (MEAI `FunctionInvokingChatClient` or a Semantic Kernel agent) behind the **same `IClassifier` port** — fetch-URL / web-search / look-up tools, then a final routing decision.
- Swap-in is transparent to the pipeline (the enrichment consumer just calls `IClassifier`).

**Architecture payoff:** drops in behind the existing port — the deferred *"agentic AI multi-step workflows"* learning objective, realised without touching consumers.

---

## Local LLM via Ollama — full data residency (NfA-P1)

**Status:** Idea — the target state of ADR 0007 / NfA-P1.
**Motivation:** Classification + embeddings currently run on cloud providers (OpenRouter / Mistral). The privacy NFR's goal is local-by-default so capture content never leaves the homelab.

### Proposed shape

- A `Local`/Ollama provider behind the existing AI provider abstraction; `Embeddings:Provider=Local` as the default.
- Add the `OutboundCallAuditTests` that asserts no cloud LLM call in the default profile.

**Architecture payoff:** one new provider adapter — no classifier or consumer change — flips FlowHub to full data residency and closes NfA-P1.

---

## Confidence-driven human-in-the-loop

**Status:** Idea.
**Motivation:** When the classifier is unsure, guessing is worse than asking.

### Proposed shape

- The classifier returns a confidence; below a threshold the capture goes to a **review queue** (or asks back via the originating channel — e.g. a Telegram inline reply) instead of auto-routing.
- The user's choice becomes a labelled example for future tuning.

**Architecture payoff:** a branch in the routing consumer + a UI list; the `ClassifierTrace`/confidence plumbing already exists on `ClassificationResult`.

---

## Semantic features on the existing pgvector index

**Status:** Idea — builds directly on ADR 0006.
**Motivation:** Embeddings are already generated and stored; surface them.

- **Related captures** — "you saved 3 similar things" on the detail page.
- **Semantic dedup** — warn on near-duplicate captures at submit time.
- **Natural-language search** over all captures (already partially present; promote to a first-class UX).

**Architecture payoff:** read-side features on infrastructure that already exists (`SearchByEmbeddingAsync`) — no new write path.

---

## Graph visualisation of vectorised captures

**Status:** Idea — the *visual* sibling of [Semantic features on the existing pgvector index](#semantic-features-on-the-existing-pgvector-index); builds on ADR 0006.
**Motivation:** Every Capture already carries an embedding, but that latent structure is invisible. A graph / semantic map turns the vector space into something you can *explore* — see clusters of related captures, nearest neighbours, and outliers at a glance, and navigate your inbox by meaning instead of by date.

### Proposed shape

Two complementary renderings, both read-only over the stored `Captures.Embedding` column:

1. **Similarity graph** — nodes = captures, edges = k-nearest-neighbours above a cosine threshold (pgvector `ORDER BY embedding <=> … LIMIT k` per node). A force-directed layout surfaces clusters/communities; edge weight = similarity.
2. **2-D semantic map** — project the high-dimensional embeddings to x/y (UMAP / t-SNE / PCA) and scatter-plot them; spatial proximity ≈ semantic similarity. Cheaper to render than a dense graph at higher counts.

- **Compute split:** server computes edges/coordinates (kNN in SQL, or a batch projection), the frontend just renders. Blazor + a JS graph lib (sigma.js / cytoscape / d3-force) or a canvas scatter.
- **Interaction:** click a node → Capture detail; colour by matched skill / project / tag; filter by time or skill.

### Open questions

- **Projection choice & stability** — PCA is stable but separates clusters weakly; UMAP/t-SNE separate well but re-runs move points (jarring on refresh). Possibly store derived 2-D coords as a column, recomputed on a cadence.
- **Compute & refresh** — kNN edges are O(n·k); fine at personal scale, precompute/cache and recompute incrementally as captures arrive. Beyond a few thousand nodes, cluster/sample before rendering.
- **Where the projection runs** — pure-.NET PCA in-process vs. an offline batch (Python `umap-learn`) writing coords back; decide if it's worth a second runtime.
- **Demo caveat** — embeddings are **disabled** on the public demo (`/search` returns 503), so this can't run live there; demonstrate via local screenshots, like the rest of the semantic-search story.

**Architecture payoff:** another read-side feature on the embedding infrastructure that already exists (`SearchByEmbeddingAsync` + the stored vector column) — a new UI page + a projection/kNN query, no new write path.

---

## Independent worker split — modular monolith → distributed

**Status:** Idea — explicitly the reversible path noted in ADR 0002.
**Motivation:** The single-operator deployment doesn't need horizontal scale, but the architecture should *prove* it can get there.

### Proposed shape

- Extract the MassTransit consumer pipeline into a separate `flowhub.worker` host (own `Program.cs` + Dockerfile + compose service); the web app publishes, the worker consumes, over the shared RabbitMQ bus.
- No code rewrite: the transport already swaps in-memory↔RabbitMQ; this is a hosting/composition change.

**Architecture payoff:** demonstrates that the modular-monolith decision (ADR 0002) was *deliberate and reversible* — the consumers can scale independently of the UI by configuration, not redesign.

---

## Multi-user / multi-tenant

**Status:** Idea.
**Motivation:** FlowHub is single-operator today; the auth seam is already designed for more.

### Proposed shape

- Replace `DemoAuthHandler` with the real OIDC flow (Authentik) already specified in the ADRs.
- Partition captures + skill credentials per user; per-user skill configuration.

**Architecture payoff:** auth is a cross-cutting adapter, not woven through the domain — the dev/demo `DemoAuthHandler` ↔ real OIDC swap is a registration change.

---

## Complete the observability story

**Status:** Idea — metrics are live; tracing is wired-but-dormant.
**Motivation:** Prometheus metrics + Grafana run today; distributed tracing and AI metrics are specified but not exported.

### Proposed shape

- Enable the OTLP trace exporter + `WithTracing` + the MassTransit instrumentation source so a Capture's full pipeline span is visible in Grafana/Tempo.
- Export `gen_ai.*` metrics (per ADR 0004) and ship the Grafana dashboard JSON.

**Architecture payoff:** OpenTelemetry is already at the composition root — this is configuration + a dashboard, completing the production-readiness picture.

---

## Terminzettel → Calendar — a photo of an appointment slip becomes a calendar event

**Status:** Idea — not scoped into any Block.
**Motivation:** You're handed a paper appointment slip (Arzt, Amt, Coiffeur) and have to retype it into your calendar by hand — exactly the friction FlowHub exists to remove. Snap a photo as a Capture; a Skill reads the slip and creates the event in **Google Calendar**, no manual entry.

### Proposed shape

1. **Image capture** — the Capture is a photo (camera / share-target / upload), reusing the image path already prepped for the paperless-ngx integration.
2. **Vision extraction** — a vision-capable model (MEAI multimodal `CompleteAsync<T>`) or OCR extracts a typed `Appointment` DTO (title, start/end, location, notes) via JSON-schema structured output (ADR 0004).
3. **Skill → Google Calendar** — a new `ISkillIntegration` creates the event via the Google Calendar API (OAuth). A confirm/edit step before the write fits the confidence-driven human-in-the-loop idea above.

### Open questions

- **Auth** — Google OAuth flow + per-user token storage; which calendar to target.
- **Extraction reliability** — date/time parsing across formats (DD.MM., handwritten), timezone handling, recurring appointments.
- **Privacy** — medical/appointment data is sensitive: keep the image and extraction out of anything that ships in the CAS bundle.
- **Generalisation** — CalDAV / Nextcloud Calendar as additional targets → a natural fit for the [Marketplace for Skills](#marketplace-for-skills) idea above.

---

## n8n Interop — FlowHub as the AI brain, n8n as the no-code glue

**Status:** Idea — not scoped into any Block.
**Motivation:** FlowHub is deliberately **"Kein IFTTT/n8n Clone — code-based, not no-code"** (Projektbeschreibung §Abgrenzung), yet the original `Idee FlowHub.md` lists n8n as an *integrated* service. Both are true: FlowHub never *becomes* n8n; it *interops* with it. FlowHub stays the **AI brain** (capture → classify → route); n8n is the **no-code long-tail glue** for the thousands of services FlowHub will never natively integrate and the recurring/scheduled automations FlowHub deliberately doesn't do. This *strengthens* the positioning rather than contradicting it. All three directions below bolt onto an existing seam — a new adapter, not a core change — so they tell a clean **bidirectional** story: FlowHub *drives* n8n (A), n8n *feeds* FlowHub (B), n8n *consumes* FlowHub's intelligence (C).

### A — n8n as downstream target (driven adapter) · *recommended first slice*

A new `N8nSkillIntegration : ISkillIntegration { Name = "n8n" }` in `FlowHub.Skills/N8n/` POSTs the classified Capture to a configured n8n **webhook URL**. The classifier learns to emit `MatchedSkill = "n8n"` for "automate this" intents (and/or as the catch-all just before the `Unhandled` terminal state). Auth: optional static header / HMAC signature on the webhook — no inbound surface to secure, so this has **no auth dependency** and reuses the dispatcher untouched.

*Real-life examples:*
- **Long-tail fan-out** — capture *"renew car insurance, policy #4471"* → classified `n8n` → one n8n flow creates a Google Calendar event **and** a Vikunja task **and** a Telegram reminder; three nodes FlowHub doesn't own.
- **Recurring automation FlowHub deliberately doesn't do** — capture a product URL → tagged `shopping` → n8n scrapes the price daily and pings Telegram on a drop. FlowHub did the *understanding*; n8n did the *scheduling*.

### B — n8n as capture source (driving adapter)

n8n ingests from anywhere (IMAP, RSS, cron, third-party webhooks) and POSTs to FlowHub's inbound capture API — `ICaptureService.SubmitAsync(..., ChannelKind.N8n)`. The submit endpoint largely exists already (`FlowHub.Api`); the real work is a **machine-to-machine auth story** (API key / signed webhook), since `DevAuthHandler` is dev-only and real OIDC lands in Block 5.

*Real-life examples:*
- **RSS firehose with a brain** — n8n polls 30 feeds and POSTs each item; instead of n8n's dumb keyword filters, *FlowHub's AI* decides what's worth keeping vs. dropping. n8n is just the cheap poller.
- **Voice / IoT** — a Home Assistant automation or a Telegram voice note hits n8n, which transcribes and POSTs to FlowHub. n8n handles the protocol mess; FlowHub stays the single classification brain.

### C — classification-as-a-service (new API surface)

Expose `IClassifier` over HTTP — `POST /api/classify` → `ClassificationResult { MatchedSkill, Tags, Title }` — so n8n workflows call FlowHub as a reusable **AI node** inside *their own* flows. Inverts the relationship; smallest standalone value, but cleanly demonstrates the hexagonal driving port reused as a public endpoint. Shares B's M2M-auth dependency.

*Real-life examples:*
- **Drop-in AI routing** — someone already has a big n8n support-email workflow; they add one HTTP node calling `/api/classify` and branch on the returned skill/tags, gaining FlowHub's tuned classifier without rebuilding their pipeline.
- **Form triage** — n8n receives a contact-form webhook, calls FlowHub to classify priority/category, then routes to the right Vikunja project — FlowHub as a "smart label" microservice.

### Shared prerequisite

**B and C both block on the same thing** — a non-dev **inbound M2M auth** mechanism (API key or signed webhook), because `DevAuthHandler` is dev-only. Tie this to **Block 5 OIDC**. **A has no such dependency** (outbound only), which is the main reason it's the recommended first slice.

**Architecture payoff:** A and B are thin adapters on the existing `ISkillIntegration` / `ICaptureService` seams; C reuses the existing `IClassifier` port as a public endpoint. Promote A to an ADR + implementation plan first; B and C follow once Block 5 auth exists.

---

## References

- ADR 0002 — Service Architecture & Async Communication (`docs/adr/0002-service-architecture-and-async-communication.md`)
- ADR 0004 — AI Integration in Services (`docs/adr/0004-ai-integration-in-services.md`)
- ADR 0006 — Vector Search (`docs/adr/0006-vector-search.md`)
- ADR 0007 — LLM Hosting (`docs/adr/0007-llm-hosting.md`)
- `source/FlowHub.Web/Pipeline/CaptureEnrichmentConsumer.cs` — current classify-only "enrichment" consumer

# FlowHub — Use Cases & Requirements

## Solution Vision

FlowHub is an intelligent integration hub that captures heterogeneous inputs (URLs, text, images) from multiple channels (Telegram, Web UI), classifies them via AI, and routes them to the correct downstream service (Wallabag, Wekan, Vikunja, Paperless-ngx, Obsidian). It automates 80% of the manual workflow the operator currently performs by hand: receive input → decide where it goes → copy-paste into the right tool.

See `Projektarbeit/Idee FlowHub.md` in the CAS Obsidian vault for the original concept and management summary.

---

## Use Cases

### UC-01: Submit a Capture via Web UI (Quick)

**Actor:** Operator
**Trigger:** Operator pastes a URL or types text in the AppBar quick-capture field.
**Precondition:** Operator is authenticated (DevAuthHandler in dev, Authentik OIDC in prod).
**Flow:**
1. Operator types or pastes content into the AppBar quick-capture field.
2. Operator presses Enter or clicks the submit icon.
3. System creates a new Capture with `source = Web`, `stage = Raw`.
4. System shows a success snackbar with a link to the Capture detail.
5. The quick-capture field clears, ready for the next input.

**Postcondition:** A new Capture exists in the system with `LifecycleStage.Raw`.
**Error:** Empty content → inline validation hint "Type something first".
**Error:** Service failure → snackbar "Capture failed: {reason}", field content preserved.

### UC-02: Submit a Capture via Web UI (Long Form)

**Actor:** Operator
**Trigger:** Operator navigates to `/captures/new`.
**Precondition:** Authenticated.
**Flow:**
1. System loads available Skills from `ISkillRegistry`.
2. Operator enters multi-line content in the text field.
3. Optionally, operator selects a Skill override from the dropdown (default: "Let AI decide").
4. Operator clicks Submit.
5. System validates (content required), then creates the Capture.
6. System shows success snackbar. Form clears; page stays for rapid multi-entry.

**Postcondition:** A new Capture exists. If Skill override was selected, the Capture may skip AI classification.
**Error:** Skill registry fails to load → dropdown disabled, "Let AI decide" forced, Submit still works.

### UC-03: Submit a Capture via Telegram

**Actor:** Operator (via Telegram bot)
**Trigger:** Operator sends a message to the FlowHub Telegram bot.
**Precondition:** Telegram bot is configured and running (Block 3+).
**Flow:**
1. Telegram bot receives the message.
2. Bot creates a Capture with `source = Telegram`, `stage = Raw`.
3. Bot confirms receipt in the chat.

**Postcondition:** Capture exists, visible in the Web UI Dashboard and Captures list.
**Note:** Not implemented in Block 2 — placeholder `source/FlowHub.Telegram/`. Once the Telegram module lands in Block 3+, the bot calls `POST /api/v1/captures` (UC-08) to submit the capture, inheriting the same validation and pipeline flow.

### UC-04: Monitor Capture health via Dashboard

**Actor:** Operator
**Trigger:** Operator opens the Dashboard (`/`).
**Flow:**
1. System loads failure counts, recent captures (10), skill health, and integration health in parallel.
2. Dashboard shows the "Needs Attention" widget with orphan and unhandled counts.
3. Dashboard shows the "Recent Captures" grid with lifecycle badges.
4. Dashboard shows Skill and Integration health cards.

**Postcondition:** Operator has an at-a-glance view of system health.
**Empty state:** No captures → "No captures yet" with a call-to-action.
**All-clear state:** Zero failures → calm "All captures routed successfully" message.

### UC-05: Browse and filter Captures

**Actor:** Operator
**Trigger:** Operator navigates to `/captures` (or clicks through from Dashboard).
**Flow:**
1. System loads all Captures.
2. If `?lc=` query param is present, the lifecycle filter pre-selects.
3. Operator filters by lifecycle stage (chip bar), channel (chip bar), and/or content text (search field).
4. Grid shows filtered results with pagination.
5. Operator clicks a row to drill into the Capture detail.

**Postcondition:** Operator can find any Capture by stage, channel, or content.

### UC-06: Inspect and act on a failed Capture

**Actor:** Operator
**Trigger:** Operator opens a Capture detail page (`/captures/{id}`) for an Orphan or Unhandled Capture.
**Flow (Orphan):**
1. System shows the failure reason alert.
2. Operator sees action buttons: Retry routing, Reassign skill, Ignore.
3. Operator clicks an action (Block 2: stubbed → snackbar "Coming in Block 3").

**Flow (Unhandled):**
1. System shows "No Skill matched" alert.
2. Operator sees action buttons: Assign skill, Ignore.
3. Operator clicks an action (Block 2: stubbed).

**Postcondition (Block 3+):** Capture is retried, reassigned, or marked as ignored. Dashboard counts update.
**Block 2:** Actions show intent but do not mutate state.

### UC-07: View Skill and Integration health

**Actor:** Operator
**Trigger:** Operator navigates to `/skills` or `/integrations`.
**Flow:**
1. System loads health data from the respective service.
2. Grid shows each Skill/Integration with status (healthy/degraded/down) and activity metrics.

**Postcondition:** Operator knows which Skills/Integrations are operational.

### UC-08: Submit a Capture via REST API

**Actor:** Non-UI client (automation script, Telegram bot module, future mobile client)
**Trigger:** Client sends `POST /api/v1/captures` with a JSON body.
**Precondition:** Client holds a valid bearer token (DevAuthHandler in dev; Authentik OIDC token in prod).
**Flow:**
1. Client sends `POST /api/v1/captures` with body `{ "content": "...", "source": "Telegram|Web|Api", "skillOverride": "<SkillId|null>" }`.
2. FluentValidation at the API boundary checks: `content` non-empty, `source` is a known enum value. On failure → `400 Bad Request` with RFC 9457 ProblemDetails body (`type` URI from `FlowHubProblemTypes`).
3. Application service creates a `Capture` with `stage = Raw`, persists it (Block 4+; in-memory stub in Block 3).
4. Service publishes `CaptureCreated` event on the internal bus.
5. API returns `201 Created` with `Location: /api/v1/captures/{id}` and the full Capture resource body.

**Postcondition:** A new Capture with `LifecycleStage.Raw` exists and a `CaptureCreated` event is on the bus, triggering the async enrichment pipeline (UC-09).
**Error:** Missing/invalid `content` → `400` ProblemDetails with `errors` map.
**Error:** Unknown `source` value → `400` ProblemDetails.
**Error:** Auth missing or invalid → `401 Unauthorized`.
**Note:** API surface documented in `docs/design/api/api-surface.md`; OpenAPI schema browsable at `/scalar` (ADR 0002).

### UC-09: AI-classify and route a Capture (async pipeline)

**Actor:** System (no human interaction)
**Trigger:** `CaptureCreated` event published on the MassTransit in-process bus.
**Precondition:** Capture exists in `Raw` stage; MassTransit bus is running.
**Flow:**
1. Bus delivers `CaptureCreated` to `CaptureEnrichmentConsumer`.
2. `CaptureEnrichmentConsumer` calls `IClassifier.ClassifyAsync(capture)`.
3. If an AI provider key is configured, `AiClassifier` sends the capture content to the configured LLM (Anthropic Claude Haiku 4.5 by default) with `MaxOutputTokens=300`, `Temperature=0.2`. If not configured, `KeywordClassifier` is used directly.
4. `IClassifier` returns a `ClassificationResult` with `SkillId` (nullable) and confidence.
5. Consumer updates the Capture's `SkillId` and sets `stage = Classified`; publishes `CaptureClassified` event.
6. Bus delivers `CaptureClassified` to `SkillRoutingConsumer`.
7. `SkillRoutingConsumer` looks up the matched `ISkillIntegration` by `SkillId`.
   - If found: calls `ISkillIntegration.HandleAsync(capture)`. On success → `stage = Routed`.
   - If no skill matched (`SkillId == null`): `stage = Orphan`.
8. On integration error, MassTransit retries per the retry policy (NF-10). After retries exhausted: `LifecycleFaultObserver` sets `stage = Unhandled` with a `FailureReason`.

**Postcondition:** Capture reaches a terminal stage (`Routed`, `Orphan`, or `Unhandled`). Dashboard counts update on next load.
**Reference:** ADR 0003 (async pipeline), ADR 0004 (AI classifier).

### UC-10: Graceful AI-classifier fallback to keyword floor

**Actor:** System (no human interaction)
**Trigger:** `AiClassifier.ClassifyAsync` encounters any failure: network exception, HTTP timeout, JSON parse error, schema-constraint violation, or generic unhandled exception.
**Precondition:** AI provider is configured; `CaptureEnrichmentConsumer` is processing a `CaptureCreated` event.
**Flow:**
1. `AiClassifier.ClassifyAsync` executes the LLM call inside a try/catch covering all exception types.
2. On any exception: `AiClassifier` logs at Warning level with EventId `3010` (`AiClassifierFellBackToKeyword`), including the exception message and Capture ID.
3. `AiClassifier` delegates immediately to `KeywordClassifier.ClassifyAsync(capture)` and returns its result. The returned `ClassificationResult` has `Title = null` (keyword classifier does not produce a human-readable title).
4. `CaptureEnrichmentConsumer` continues normally — it receives a valid `ClassificationResult` regardless of which classifier produced it.

**Postcondition:** The Capture is always classified and the pipeline continues. An AI provider outage degrades classification quality but does not cause availability loss. The Warning log at EventId 3010 provides operational visibility.
**Note:** Fallback rate target and monitoring defined in NF-11. Reference: ADR 0004, decision D5.

### UC-11: Retry a failed Capture from the dashboard

**Actor:** Operator
**Trigger:** Operator opens a Capture detail page (`/captures/{id}`) for a Capture in `Orphan` or `Unhandled` stage and clicks "Retry".
**Precondition:** Capture is in `Orphan` or `Unhandled` stage. Operator is authenticated.
**Flow:**
1. UI calls `POST /api/v1/captures/{id}/retry` (API surface from UC-08's REST layer).
2. Application service resets `stage` to `Raw`, clears `FailureReason`.
3. Service publishes a new `CaptureCreated` event for the existing Capture ID.
4. The full async pipeline (UC-09) runs again from the start.
5. API returns `202 Accepted`. UI shows snackbar "Capture queued for retry" and reloads the detail page.

**Postcondition:** Capture re-enters the enrichment pipeline. The Lifecycle stage is eventually updated to `Routed`, `Orphan`, or `Unhandled` again based on pipeline outcome.
**Error:** Capture not found → `404` ProblemDetails.
**Error:** Capture is not in a retryable stage (`Raw`, `Classified`, `Routed`) → `409 Conflict` ProblemDetails.
**Note:** UC-06 documents the UI entry point and the Block-2 stub state. UC-11 defines the Block-3 implementation that fulfils the "Retry routing" action stubbed in UC-06 Flow (Orphan) step 3 and Flow (Unhandled) step 2.

### UC-12: Filter Captures by Lifecycle Stage

**Actor:** Operator  
**Trigger:** Operator selects one or more stages in the Captures list filter.  
**Flow:** System queries `ICaptureService.ListAsync` with the selected stages. List refreshes to show only matching captures.  
**Acceptance:** Selecting "Orphan" returns only orphaned captures. Selecting multiple stages returns the union.

### UC-13: Filter Captures by Tag

**Actor:** Operator  
**Trigger:** Operator enters a tag value in the tag filter field.  
**Flow:** System queries `ICaptureService.ListAsync` with `CaptureFilter.Tag` set. Returns only captures that have the tag attached.  
**Acceptance:** A capture tagged "dotnet" appears when filter is "dotnet". Captures without the tag are excluded.

### UC-14: Search Captures by Content or Title

**Actor:** Operator  
**Trigger:** Operator types a search term in the search box.  
**Flow:** System queries with `CaptureFilter.SearchTerm`. PostgreSQL ILIKE match against Content and Title (case-insensitive). Not full-text search — deferred to Block 5.  
**Acceptance:** Searching "hexagonal" returns captures whose Content or Title contains that substring (any casing).

### UC-15: View Skill-Run History for a Capture

**Actor:** Operator  
**Trigger:** Operator opens a Capture detail and views the routing history tab.  
**Flow:** System queries `ISkillRunRepository.GetByCaptureIdAsync`. Returns all routing attempts for that capture ordered by StartedAt DESC.  
**Acceptance:** A capture that was routed twice shows two SkillRun entries.

### UC-16: View Integration Health History

**Actor:** Operator  
**Trigger:** Operator opens the Integrations page and expands an integration's history.  
**Flow:** System queries `IIntegrationRepository.GetRecentSamplesAsync`. Returns the last N health samples.  
**Acceptance:** Shows SampledAt, Status, and DurationMs for each sample.

---

## Non-Functional Requirements (SMART)

| ID | Requirement | Measurable target | Verified by |
|---|---|---|---|
| NF-01 | **Response time** — Dashboard loads within acceptable time on a single-operator workstation | < 500 ms from navigation to all 4 cards rendered (on localhost with Bogus stubs) | Manual observation, future Playwright timing |
| NF-02 | **Availability** — FlowHub runs as a self-hosted service in the operator's homelab | 99% uptime during operator's active hours (not 24/7 SLA) | Docker healthcheck in Block 5 |
| NF-03 | **Concurrency** — system supports a single concurrent operator | 1 concurrent SignalR circuit without degradation | Architecture (Interactive Server, no horizontal scaling needed) |
| NF-04 | **Security** — all pages require authentication | 0 pages accessible without a valid auth session | `[Authorize]` on pages + DevAuthHandler in dev, OIDC in prod |
| NF-05 | **Testability** — all UI components testable in isolation | 100% of page components renderable in bUnit without a running server | bUnit test suite (currently 31 tests) |
| NF-06 | **Maintainability** — code compiles with zero warnings | `TreatWarningsAsErrors=true` in `Directory.Build.props` | `make build` in CI |
| NF-07 | **Portability** — runs on Linux (homelab Docker) and WSL2 (dev) | `make run` works on both environments | Manual verification |
| NF-08 | **Data privacy** — no Capture content leaves the operator's infrastructure | 0 external API calls for data processing (AI classification runs locally via Ollama in future blocks) | Architecture review |
| NF-09 | **API latency** — REST endpoints respond within tight bounds for an interactive integration hub | `POST /api/v1/captures` p95 < 200 ms (server-side, excluding async bus-publish); `GET /api/v1/captures` p95 < 100 ms with cursor pagination | Block 5 load test (k6/nbomber); Block 3 evidence: integration-test wall-clock < 1 s end-to-end against in-memory stubs |
| NF-10 | **Async pipeline retry budget** — transient failures in the enrichment, embedding, or routing consumers are retried before a Capture is marked `Unhandled` | `CaptureEnrichmentConsumer`: 2 retries at 100 ms / 500 ms; `CaptureEmbeddingConsumer`: 3 retries at 500 ms / 2 000 ms / 5 000 ms (failure leaves the Capture without an embedding — backfill via admin rebuild); `SkillRoutingConsumer`: 3 retries at 500 ms / 2 000 ms / 5 000 ms; after exhaustion `LifecycleFaultObserver` marks `Unhandled` | MassTransit `TestHarness` tests in `tests/FlowHub.Web.ComponentTests/Pipeline/` (6 harness tests) |
| NF-11 | **AI classifier fallback rate** — an AI provider outage must not propagate to availability loss | < 5% of classifications fall back to keyword during normal provider availability (Anthropic Haiku 4.5 SLA ~99.5%); fallback always succeeds — `AiClassifier` never throws to the caller | EventId 3010 log volume in production; `make test-ai` live integration runs demonstrate the success path; ADR 0004 D5 |
| NF-12 | **AI classification cost** — per-capture cost is sub-cent to keep the homelab budget bounded | `MaxOutputTokens=300`, `Temperature=0.2`; estimated ~200 tokens input + ~150 tokens output → < $0.001 per classification on Haiku 4.5 pricing | Anthropic dashboard usage report from operator runs; cost guard configured in `AddFlowHubAi(IConfiguration)`; ADR 0004 §"Cost guards" |
| NF-13 | **OpenAPI versioning SLA** — the REST API is URL-versioned from day one so clients are not broken by future changes | Breaking changes land only in a new major version (`/api/v2/...`); v1 is retained for at least one major-version overlap period | ADR 0002 D6; endpoint catalogue in `docs/design/api/api-surface.md`; version prefix verified in route registration |

## UC-10: Deploy via Docker Compose

**Actor:** Operator  
**Precondition:** Docker and Docker Compose v2 are installed.  
**Steps:**
1. Operator creates `.env` file with required env vars (DB credentials, optional OIDC config, optional embedding key).
2. Operator runs `docker compose up -d`.
3. `flowhub.migrations` service applies EF Core migrations and exits successfully.
4. `flowhub.web` starts, connects to PostgreSQL and RabbitMQ, exposes port 5070.
5. Operator opens `http://localhost:5070`.  
**Postcondition:** FlowHub is running; all dependencies healthy.  
**Exception:** If migrations fail, `flowhub.web` does not start (depends_on condition).

## UC-11: Semantic Search for Captures

**Actor:** Operator  
**Precondition:** `Embeddings__ApiKey` is configured; at least one Capture exists with a stored embedding.  
**Note on embedding generation:** Embeddings for new Captures are generated asynchronously by `CaptureEmbeddingConsumer` (subscribed to `CaptureCreated`), so a Capture submitted via UC-09 becomes searchable a moment after submission, not synchronously with the `POST /api/v1/captures` response. This keeps capture submission inside the NF-09 p95 < 200 ms budget regardless of embedding-provider latency. Backfill of captures stored before the provider was configured is via `POST /api/v1/admin/embeddings/rebuild`.

**Steps:**
1. Operator sends `GET /api/v1/captures/search?q=database+performance&limit=5`.
2. FlowHub embeds the query via the configured embedding provider.
3. FlowHub returns up to 5 Captures ranked by cosine similarity.  
**Postcondition:** Response contains Captures semantically related to the query, even if query words don't appear in titles or content.  
**Exceptions:**
- Empty/whitespace `q` → `400 Bad Request` (ValidationProblem).
- Embedding service not configured → `503 Service Unavailable` with ProblemDetails body.
- `limit` is clamped to 1..200 (mirrors the list-endpoint contract).

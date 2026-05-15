# Acceptance Criteria

Acceptance criteria per use case in Given/When/Then form. Each row maps to a specific use case in [`use-cases.md`](use-cases.md). Where an automated test covers the behaviour, the test project is referenced.

## Capture submission

### AC-01 — Submit Capture via Web UI (Quick) → UC-01

- **Given** the user is on the Dashboard or any page with the QuickCaptureField
- **When** they type free-form text and press Enter (or `Ctrl+Enter`)
- **Then** a new Capture is persisted with `Stage = Pending`, `ChannelKind = WebUiQuick`, and the field clears
- **And** the new Capture appears at the top of the recent-captures list within 2 seconds
- *Test: `FlowHub.Web.ComponentTests/QuickCaptureFieldTests.cs`*

### AC-02 — Submit Capture via Web UI (Long Form) → UC-02

- **Given** the user opens the "New Capture" page
- **When** they fill title, content, optional tags, then click "Save"
- **Then** the Capture is persisted with the provided fields and `Stage = Pending`
- **And** the user is redirected to the Capture detail view
- **And** validation rejects empty title or content > 64 KB (ProblemDetails 400)

### AC-03 — Submit Capture via Telegram → UC-03

- **Given** the Telegram bot is wired and the sender's chat ID is allowlisted
- **When** the sender posts a text or URL message to the bot
- **Then** the bot calls the REST API and creates a Capture with `ChannelKind = Telegram`, `ExternalId = <chat>:<message>`
- **And** the bot replies with a confirmation message containing the Capture ID
- **And** non-allowlisted senders receive a "not authorised" reply and no Capture is created

### AC-08 — Submit Capture via REST API → UC-08

- **Given** the caller holds a valid bearer token
- **When** they `POST /api/v1/captures` with a valid JSON body
- **Then** the response is `201 Created` with `Location: /api/v1/captures/{id}` and the persisted Capture body
- **And** invalid payloads return `400 Bad Request` with an RFC 9457 ProblemDetails document
- *Test: `FlowHub.Api.IntegrationTests/CapturesEndpointTests.cs`*

## Capture lifecycle & routing

### AC-09 — AI-classify and route a Capture → UC-09

- **Given** a Capture exists in `Stage = Pending`
- **When** the `CaptureEnrichmentConsumer` processes it
- **Then** a classification result (matched skill or null) is persisted within 5 s under normal load
- **And** a `SkillRouting` event is published to MassTransit
- **And** the Capture transitions to `Stage = Routed` (or `Stage = Unmatched` if no skill applies)
- *Tests: MassTransit harness in `FlowHub.Api.IntegrationTests`*

### AC-10 — Graceful fallback to keyword classifier → UC-10

- **Given** the LLM provider is unreachable or no API key is configured
- **When** a Capture is processed
- **Then** the `KeywordClassifier` runs and the Capture is still classified (best-effort)
- **And** a structured log line records `Classifier=Keyword,Reason=LlmUnavailable`
- **And** no DLQ event is produced

### AC-11 — Retry a failed Capture → UC-11

- **Given** a Capture is in `Stage = Failed`
- **When** the user clicks "Retry" on the Capture-detail page
- **Then** the Capture is re-published to the enrichment queue
- **And** a new `SkillRun` is appended to its history
- **And** retry is rate-limited to ≤ 3 attempts per Capture per hour (returns 429 above limit)

## Dashboard, lists & filters

### AC-04 — Monitor Capture health via Dashboard → UC-04

- **Given** Captures exist across all lifecycle stages
- **When** the user opens the Dashboard
- **Then** counts per `LifecycleStage` are displayed in dedicated cards
- **And** the "Recent Captures" card shows the 10 newest entries
- **And** the page renders within 1 second on a 10k-row dataset (NfA-P1)

### AC-05 / AC-12 / AC-13 — Browse, filter by stage, filter by tag → UC-05, UC-12, UC-13

- **Given** the user is on the Captures list
- **When** they select a `Stage` chip or type a tag in the search bar
- **Then** the result set updates client-side within 200 ms (or with a debounced server query)
- **And** pagination uses cursor-based paging — no `OFFSET` over large result sets
- **And** an empty filter result shows an empty-state message, not an error

### AC-06 — Inspect and act on a failed Capture → UC-06

- **Given** a Capture in `Stage = Failed`
- **When** the user opens the Capture detail page
- **Then** the failure reason, retry count, and last error are visible
- **And** the user can Retry, Mark-as-done, or Delete from the page
- **And** every action emits a domain event captured in the SkillRun history

### AC-14 — Search Captures by content or title → UC-14

- **Given** Captures contain title and content text
- **When** the user enters a non-empty query
- **Then** matching Captures are returned ranked by `ts_rank` (PostgreSQL full-text)
- **And** results render within 500 ms at p95 over the volume envelope in NfA-S1

### AC-18 — Semantic search → UC-18

- **Given** Captures have embeddings stored (`vector(1024)`)
- **When** the user calls `GET /api/v1/captures/search?q=…&mode=semantic`
- **Then** the top-N nearest Captures are returned ordered by cosine distance
- **And** the response includes the distance score per item
- **And** the endpoint completes in ≤ 200 ms p95 at the NfA-S1 volume

## Health & history

### AC-07 — View Skill and Integration health → UC-07

- **Given** integrations have been exercised (or stubbed)
- **When** the user opens the Skills/Integrations health view
- **Then** each integration shows current status (`Healthy / Degraded / Down`), last sample time, and a 24 h sparkline
- **And** "down" badges are reachable via keyboard (a11y)

### AC-15 — View Skill-Run history → UC-15

- **Given** a Capture has at least one SkillRun
- **When** the user opens its detail page
- **Then** all SkillRuns are listed chronologically with status, duration, and integration name
- **And** clicking a SkillRun opens the structured-log excerpt (TraceId-correlated)

### AC-16 — View Integration health history → UC-16

- **Given** `IntegrationHealthSamples` exist for an integration
- **When** the user opens the integration detail view
- **Then** a chart shows samples over the last 24 h / 7 d / 30 d
- **And** missing samples are visually distinct (gap, not interpolation)

## Operations

### AC-17 — Deploy via Docker Compose → UC-17

- **Given** the operator has Docker Compose v2 and a `.env` file with secrets
- **When** they run `docker compose up -d`
- **Then** `flowhub.web`, `postgres`, `rabbitmq`, `prometheus`, `grafana` become healthy within 60 s
- **And** the EF migrations init container exits 0 before `flowhub.web` reports ready
- **And** `https://<host>/health/live` and `/metrics` are reachable from the host
- *Runbook: [`docs/runbooks/v0.1.0-final-acceptance.md`](../runbooks/v0.1.0-final-acceptance.md) (final submission state); historical Block-4 milestone in [`beta-mvp-acceptance.md`](../runbooks/beta-mvp-acceptance.md)*

---

## Submission acceptance (Block 5 release)

For the final submission tag `v0.1.0`, the following must be true:

- [ ] All 234 tests green in CI (`ci.yml` workflow run linked from SUBMISSION.md §3.3)
- [ ] `docker compose up -d` produces a healthy stack on a clean host within 60 s (AC-17)
- [ ] `GET /health/live` returns 200 within 30 s of cold start (NfA-A1)
- [ ] `GET /metrics` returns Prometheus-format metrics (NfA-O1)
- [ ] At least one Capture can be created via Telegram and routed to a real skill (AC-03 + AC-09 happy path)
- [ ] Semantic search returns ranked results on a 1k-Capture seed dataset (AC-18)
- [ ] No secret value is present in any committed file (`gitleaks` green — NfA-SE2)

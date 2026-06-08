# FlowHub — Acceptance Criteria

Consolidated acceptance criteria for FlowHub's user-facing and system-facing behavior. Each criterion is testable and maps to a specific use case in `docs/spec/use-cases.md` and to the test, runbook, or live evidence that verifies it.

This document is the single source of truth for "what counts as done" per feature. The individual `Akzeptanzkriterien:` blocks inside `use-cases.md` remain authoritative per UC; this file aggregates them for review, grading, and submission.

**Conventions**

- **AC-XX-N** — acceptance criterion N for UC-XX.
- **Verified by** — concrete artifact: a test class, `make` target, runbook step, or Bruno collection. If a criterion is currently deferred (Block 2 stub state, future block), it is marked **(deferred)** with the block where it will be verified.
- Non-functional requirements live in `docs/spec/nfa.md` as the SMART-decomposed `NfA-*` catalogue (the successor to the `NF-01 … NF-13` quality table in `docs/spec/use-cases.md`; see the mapping note there). They are not duplicated here.

---

## 1. Capture submission (UI + API)

### UC-01 — Quick capture via Web UI AppBar

| ID | Criterion | Verified by |
|---|---|---|
| AC-01-1 | After typing content and pressing Enter, a `Captured ✓` snackbar appears within 2 s. | Playwright `HappyFlowTests.QuickCapture_TodoEntry_AppearsInCapturesListAndDetail` |
| AC-01-2 | The captured row appears on `/captures` and the detail page renders the same content. | Same Playwright test |
| AC-01-3 | Empty content → no row created; inline hint visible. | bUnit `AppBarQuickCaptureTests` |

### UC-02 — Long-form capture via `/captures/new`

| ID | Criterion | Verified by |
|---|---|---|
| AC-02-1 | `/captures/new` renders a multi-line text area and a Skill dropdown. | bUnit `NewCapturePageTests` |
| AC-02-2 | Submitting non-empty content yields a 201-equivalent service call and a success snackbar; the form clears for the next entry. | bUnit `NewCapturePageTests` |
| AC-02-3 | If `ISkillRegistry.ListAsync` throws, the dropdown is disabled and a `"Let AI decide"` placeholder is forced. | bUnit `NewCapturePageTests` |

### UC-03 — Capture via Telegram

| ID | Criterion | Verified by |
|---|---|---|
| AC-03-1 | Bot reply confirms receipt within 5 s of the user message. | **(deferred — Block 3+)** |
| AC-03-2 | Capture appears in `/captures` with `source = Telegram` and the bot user's identifier in metadata. | **(deferred — Block 3+)** |
| AC-03-3 | Telegram-originated submissions inherit the same validation and pipeline flow as the API path (UC-08). | Covered by UC-08 ACs once the Telegram module lands. |

### UC-08 — Submit a Capture via REST API

| ID | Criterion | Verified by |
|---|---|---|
| AC-08-1 | `POST /api/v1/captures` with a valid body returns 201 within NF-09 (p95 < 200 ms server-side). | `just smoke-prod` step [5/6]; `tests/FlowHub.Api.IntegrationTests/` |
| AC-08-2 | Response body matches the `Capture` schema and `Location: /api/v1/captures/{id}` header is present. | `tests/FlowHub.Api.IntegrationTests/CapturesEndpointTests.cs` |
| AC-08-3 | Missing `content` / unknown `source` → 400 ValidationProblem (`type` = `validation.md`). | `tests/FlowHub.Api.IntegrationTests/CapturesEndpointTests.cs` |
| AC-08-4 | Bruno collection `bruno/captures/submit-capture.bru` round-trips against a live stack. | `just smoke-prod`; manual Bruno run |

---

## 2. Async pipeline (classification + routing)

### UC-09 — AI-classify and route a Capture

| ID | Criterion | Verified by |
|---|---|---|
| AC-09-1 | A URL-content Capture reaches `Completed` with `MatchedSkill = "Wallabag"` and a non-empty `ExternalRef`. | `Skills.ContractTests`; `just test-beta` |
| AC-09-2 | A todo-content Capture reaches `Completed` with `MatchedSkill = "Vikunja"` and a non-empty `ExternalRef`. | `Skills.ContractTests`; `just test-beta` |
| AC-09-3 | MassTransit harness tests prove the three consumer hops fire in order (Created → Classified → Routed). | `tests/FlowHub.Web.ComponentTests/Pipeline/*` (6 harness tests) |

### UC-10 — Graceful AI-classifier fallback

| ID | Criterion | Verified by |
|---|---|---|
| AC-10-1 | With `Ai__OpenRouter__ApiKey` deliberately invalid, a Capture submitted via UC-08 still reaches `Classified` stage via `KeywordClassifier`. | `tests/FlowHub.Web.ComponentTests/Ai/AiClassifierTests.cs` |
| AC-10-2 | EventId `3010 AiClassifierFellBackToKeyword` is logged at Warning level with the exception type and Capture ID. | Same test class |
| AC-10-3 | `AiClassifier` never throws to its caller — `ClassifyAsync` always returns a `ClassificationResult`. | Same test class |

### UC-11 — Retry a failed Capture

| ID | Criterion | Verified by |
|---|---|---|
| AC-11-1 | `POST /api/v1/captures/{id}/retry` on an Orphan Capture returns 202 Accepted with `stage = Raw`, `failureReason = null`. | `tests/FlowHub.Api.IntegrationTests/CaptureRetryEndpointTests.cs` |
| AC-11-2 | Same call on a `Completed` Capture returns 409 (`type` = `capture-not-retryable.md`). | Same test class |
| AC-11-3 | Same call with an unknown id returns 404 (`type` = `capture-not-found.md`). | Same test class |

---

## 3. Browse, filter, search

### UC-04 — Dashboard

| ID | Criterion | Verified by |
|---|---|---|
| AC-04-1 | The Dashboard renders within NF-08 budget (`p95 < 1.5 s` first paint on a 100-capture DB). | Manual perf check; future Playwright timing |
| AC-04-2 | Empty DB shows the empty-state message. | bUnit `DashboardTests` |
| AC-04-3 | Mixed-state DB shows non-zero Orphan / Unhandled counts in the "Needs Attention" widget. | bUnit `DashboardTests` |

### UC-05 — Browse and filter Captures

| ID | Criterion | Verified by |
|---|---|---|
| AC-05-1 | Deep link `/captures?lc=Orphan` pre-selects the Orphan chip and shows only Orphan rows. | bUnit `CapturesListPageTests` |
| AC-05-2 | Combining a lifecycle chip + a channel chip applies an AND filter. | bUnit `CapturesListPageTests` |
| AC-05-3 | The grid pages at 10/25/50 rows (configurable, default 10). | bUnit `CapturesListPageTests` |

### UC-06 — Inspect a failed Capture

| ID | Criterion | Verified by |
|---|---|---|
| AC-06-1 | Detail page shows the `FailureReason` of an Orphan and "No Skill matched" for an Unhandled. | bUnit `CaptureDetailPageTests` |
| AC-06-2 | "Retry" button is enabled only for Orphan / Unhandled stages (UC-11 implements the action). | bUnit `CaptureDetailPageTests` |
| AC-06-3 | Block 2 stubs trigger a "Coming in Block 3" snackbar instead of mutating state. | bUnit `CaptureDetailPageTests` **(historical — superseded by UC-11 in Block 3)** |

### UC-12 — Filter by Lifecycle Stage

| ID | Criterion | Verified by |
|---|---|---|
| AC-12-1 | Selecting "Orphan" returns only orphaned captures. | `tests/FlowHub.Persistence.Tests/CaptureRepositoryTests.cs` |
| AC-12-2 | Selecting multiple stages returns the union. | Same test class |

### UC-13 — Filter by Tag

| ID | Criterion | Verified by |
|---|---|---|
| AC-13-1 | A capture tagged "dotnet" appears when filter is "dotnet". | `CaptureRepositoryTests` |
| AC-13-2 | Captures without the tag are excluded. | Same |

### UC-14 — Search by Content or Title

| ID | Criterion | Verified by |
|---|---|---|
| AC-14-1 | Searching "hexagonal" returns captures whose Content or Title contains that substring (case-insensitive ILIKE). | `CaptureRepositoryTests` |
| AC-14-2 | Full-text search is explicitly deferred — substring match is the documented current behavior. | ADR-0005 §"Search scope" |

### UC-18 — Semantic Search

| ID | Criterion | Verified by |
|---|---|---|
| AC-18-1 | `POST /api/v1/captures` followed by polling `Captures.Embedding` populates the column within 30 s when `Embeddings__ApiKey` is set (actual ~2 s). | `just smoke-prod` step [6/6] |
| AC-18-2 | `GET /api/v1/captures/search?q=...` with a non-empty `q` returns 200 with an array body when at least one embedded Capture exists. | `tests/FlowHub.Api.IntegrationTests/SemanticSearchEndpointTests.cs` |
| AC-18-3 | Empty `q` returns 400 ValidationProblem; with `Embeddings__ApiKey` unset, returns 503 ProblemDetails. | Same test class |
| AC-18-4 | `POST /api/v1/admin/embeddings/rebuild` returns 200 `{ processed, skipped, failed }` when keys are present, 503 otherwise. | Same test class |

---

## 4. Skill & Integration observability

### UC-07 — Health pages

| ID | Criterion | Verified by |
|---|---|---|
| AC-07-1 | `/skills` renders one row per registered Skill with status badge (healthy/degraded/down). | bUnit `SkillsPageTests` |
| AC-07-2 | `/integrations` renders one row per `ISkillIntegration` adapter with `LastWriteAt` + `LastWriteDurationMs`. | bUnit `IntegrationsPageTests` |
| AC-07-3 | Each row exposes a "history" expansion showing recent samples (UC-16). | Same |

### UC-15 — Skill-Run history per Capture

| ID | Criterion | Verified by |
|---|---|---|
| AC-15-1 | A capture that was routed twice shows two SkillRun entries ordered by `StartedAt DESC`. | `tests/FlowHub.Persistence.Tests/SkillRunRepositoryTests.cs` |

### UC-16 — Integration health history

| ID | Criterion | Verified by |
|---|---|---|
| AC-16-1 | Shows `SampledAt`, `Status`, and `DurationMs` for each sample. | `IntegrationRepositoryTests` |

---

## 5. Deployment

### UC-17 — Deploy via Docker Compose

| ID | Criterion | Verified by |
|---|---|---|
| AC-17-1 | `docker compose up --build -d --wait` returns exit 0 with all `service_healthy` dependencies satisfied. | `just smoke-prod` step [1/6] |
| AC-17-2 | `flowhub.migrations` container reaches `service_completed_successfully` with exit code 0. | `just smoke-prod` step [1/6] |
| AC-17-3 | `GET /health/live` from inside the compose network returns 200 within NF-D3 (30 s) of container start. | `just smoke-prod` step [2/6] |
| AC-17-4 | `GET /metrics` returns a Prometheus exposition containing at least one `^dotnet_` and one `^http_` series. | `just smoke-prod` step [3/6] |

---

## Coverage summary

| Use case | ACs defined | Implemented & verified | Deferred / historical |
|---|--:|--:|--:|
| UC-01 quick capture | 3 | 3 | 0 |
| UC-02 long-form capture | 3 | 3 | 0 |
| UC-03 Telegram | 3 | 0 | 3 (post-Block-3) |
| UC-04 dashboard | 3 | 3 | 0 |
| UC-05 browse/filter | 3 | 3 | 0 |
| UC-06 failed-capture detail | 3 | 2 | 1 (Block-2 stub) |
| UC-07 health pages | 3 | 3 | 0 |
| UC-08 REST API submit | 4 | 4 | 0 |
| UC-09 async pipeline | 3 | 3 | 0 |
| UC-10 AI fallback | 3 | 3 | 0 |
| UC-11 retry | 3 | 3 | 0 |
| UC-12 filter by stage | 2 | 2 | 0 |
| UC-13 filter by tag | 2 | 2 | 0 |
| UC-14 substring search | 2 | 2 | 0 |
| UC-15 skill-run history | 1 | 1 | 0 |
| UC-16 integration history | 1 | 1 | 0 |
| UC-17 docker compose | 4 | 4 | 0 |
| UC-18 semantic search | 4 | 4 | 0 |
| **Total** | **50** | **46** | **4** |

Deferred ACs are all Block-3+ scope (Telegram channel implementation) or superseded historical stubs — none represent regressions or unverified production behavior.

---

## Persistence-Layer Coverage (Block 4)

The persistence layer underlies multiple user-facing UCs. The table below maps each Block-4-relevant UC to the Persistence test class that owns its data-access ACs, so the persistence acceptance criteria can be traced as a coherent set rather than scattered across the per-UC sections above.

| UC | What persistence has to deliver | Verified by (`tests/FlowHub.Persistence.Tests/`) |
|---|---|---|
| UC-05 — Browse/filter Captures | Cursor pagination (`CreatedAt DESC, Id DESC`); `.Include(Tags)` round-trip; no N+1 | `EfCaptureRepositoryTests` |
| UC-09 — Async pipeline (Classified stage) | `MarkClassifiedAsync` persists `MatchedSkill`, `Title`, `VikunjaProject` atomically | `EfCaptureServiceTests` |
| UC-11 — Retry a failed Capture | Stage reset (`Failed → Raw`); idempotent re-publish; no duplicate `SkillRun` rows | `EfCaptureServiceTests`, `EfSkillRunRepositoryTests` |
| UC-12 — Filter by Lifecycle Stage | Single-stage and multi-stage union filters on `IX_Captures_Stage` | `EfCaptureRepositoryTests` (AC-12-1, AC-12-2) |
| UC-13 — Filter by Tag | M-N join via `CaptureTag`; tag-name filter | `EfCaptureRepositoryTests` (AC-13-1, AC-13-2) |
| UC-14 — Substring search | PostgreSQL `ILIKE` over `Content` and `Title` | `EfCaptureRepositoryTests` (AC-14-1, AC-14-2) |
| UC-15 — Skill-Run history per Capture | `SkillRun` rows ordered by `StartedAt DESC`; one row per skill invocation | `EfSkillRunRepositoryTests` (AC-15-1) |
| UC-16 — Integration health history | Latest-per-integration sample query on `(IntegrationName, SampledAt DESC)` index | `EfIntegrationHealthServiceTests` (AC-16-1) |
| UC-18 — Semantic search | pgvector `vector(1024)` column + HNSW index; `FromSqlRaw` with float-array literal | `EfCaptureRepositoryTests.SemanticSearch_*` |
| **All migrations** | `0001_Initial` through `0008_AddVikunjaProjectToCapture` apply cleanly on an empty DB | `MigrationSmokeTest` |

All Persistence tests run against real PostgreSQL 17 via Testcontainers — no in-memory provider drift. The 29 tests in the project are listed by class in `docs/insights/block-4.md` § "Persistence-Layer Coverage".

---

## Cross-references

- Use cases (authoritative per-UC narrative): `docs/spec/use-cases.md`
- Non-functional requirements: `docs/spec/nfa.md`
- Testing strategy and tooling: `docs/spec/testing-strategy.md`
- ADRs: `docs/adr/0001..0006` (ADR 0005 = Persistence design)
- Smoke-test runbook: `just smoke-prod` (defined in `justfile`)
- Beta-MVP operator runbook: `docs/runbooks/beta-mvp-acceptance.md`
- Block-4 insights (test counts + per-class coverage): `docs/insights/block-4.md`

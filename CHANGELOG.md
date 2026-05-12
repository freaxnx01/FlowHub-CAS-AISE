# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0] — 2026-05-12

CAS AISE Abgabe-Version. Consolidates all five course blocks (Einführung, Frontend, Service, Persistence, Deployment) plus the post-Block-5 production-smoke + rubric-gap pass. Repository version is `0.1.0` per `Directory.Build.props`.



### Added

- PostgreSQL provider (Npgsql 10.0.1) replaces SQLite throughout `FlowHub.Persistence`
- `ICaptureRepository` + `EfCaptureRepository` — repository interface in Core, EF Core impl in Persistence
- `IChannelRepository` + `EfChannelRepository`, `ISkillRepository` + `EfSkillRepository`
- `IIntegrationRepository` + `EfIntegrationRepository`, `ITagRepository` + `EfTagRepository`
- `ISkillRunRepository` + `EfSkillRunRepository`
- `EfSkillRegistry` (replaces `SkillRegistryStub` in DI), `EfIntegrationHealthService` (replaces `IntegrationHealthServiceStub` in DI)
- `CaptureQueryBuilder` — expression-tree filter supporting Stage, Source, Tag, SearchTerm (ILike), cursor
- EF Core migrations: `0001_Initial` (PostgreSQL), `0002_AddChannelAndSkill`, `0003_Block4FullDomain`
- Testcontainers PostgreSQL test infrastructure (`PostgresFixture`, per-test isolated databases)
- `EfCaptureRepositoryTests` (9 tests), `EfSkillRegistryTests` (2 tests), `EfIntegrationHealthServiceTests` (3 tests), `MigrationSmokeTest` (2 tests) — 16 new integration tests via Testcontainers
- Docker Compose: `flowhub.migrations` init service (12-Factor XII pattern)
- `make db-up` and `make db-migrate` Makefile targets
- `docs/spec/nfa.md` — SMART NfA criteria (NfA-01 through NfA-05)
- `docs/design/db/er.md` — full ER diagram (7 tables, FK strategy documented)
- `docs/spec/use-cases.md` — UC-09 through UC-16 (persistence + filter use cases)
- **Project scaffolding**: `global.json` (.NET 10), `Directory.Build.props`, `Directory.Packages.props` (central package management), `FlowHub.slnx` root solution, `Makefile` with dev task targets
- **FlowHub.Core**: domain types (`Capture`, `ChannelKind`, `LifecycleStage` with `Completed` terminal state, `FailureCounts`, `SkillHealth`, `IntegrationHealth`, `HealthStatus`) and driving-port interfaces (`ICaptureService`, `ISkillRegistry`, `IIntegrationHealthService`)
- **FlowHub.Web**: Blazor Web App with Interactive Server rendering (per ADR 0001)
  - MudBlazor wiring + `MudLayout` shell (AppBar with QuickCaptureField, mini MudDrawer, MudMainContent)
  - `DevAuthHandler` — dev-only auth bypass returning fixed "Dev Operator" principal
  - Bogus-backed stub services (`CaptureServiceStub`, `SkillRegistryStub`, `IntegrationHealthServiceStub`) with seeded test data including orphan/unhandled/routed/completed lifecycle states
  - **Dashboard** (`/`): NeedsAttentionCard, RecentCapturesCard (MudDataGrid, 10 rows), SkillHealthCard, IntegrationHealthCard — with loading, empty, all-clear, and error states
  - **New Capture** (`/captures/new`): multi-line content + optional Skill override dropdown, rapid multi-entry (clear form + stay on page after submit)
  - **Captures list** (`/captures`): full-page MudDataGrid with lifecycle chip filter, channel chip filter, text search, pagination; `?lc=` query param pre-selection from Dashboard click-through
  - **Capture detail** (`/captures/{id}`): single-card view with full content, metadata, conditional failure alert (Orphan/Unhandled), and stubbed action buttons (Retry/Reassign/Assign/Ignore → "Coming in Block 3" snackbar)
  - **Skills** (`/skills`): MudDataGrid with name, HealthDot status, routed-today count
  - **Integrations** (`/integrations`): MudDataGrid with name, HealthDot status, last-write timing
  - Shared components: `LifecycleBadge`, `HealthDot`
  - `<NotFound>` template in Routes.razor for unmatched URLs
- **FlowHub.Web.ComponentTests**: bUnit + xunit + FluentAssertions + NSubstitute — 17 tests covering Dashboard cards, stub service behavior, New Capture form, and Captures list rendering
- **ADR 0001**: Frontend render mode and architecture (Blazor Interactive Server, OIDC/Authentik, Web as Channel, Bogus stubs)
- **FlowHub Glossary** in the CAS Obsidian vault: Capture (with 6-stage lifecycle), Channel, Skill, Integration, Enrichment, Page/Component/Layout/Card/Widget, Render Mode
- **Design docs**: wireframes + Mermaid flow diagrams for Dashboard, New Capture, Captures list, and Capture detail in `docs/design/`
- **Block 3 Slice C — AI integration**: `FlowHub.AI` becomes an active project with `AiClassifier` (`IClassifier` adapter using `Microsoft.Extensions.AI`)
  - Two interchangeable provider adapters behind one `IChatClient`: Anthropic native (default `claude-haiku-4-5-20251001`) and OpenRouter (default `meta-llama/llama-3.1-70b-instruct`)
  - One round-trip for classification + AI-generated `Title` (extends `ClassificationResult` with optional `Title?`)
  - Graceful fallback to `KeywordClassifier` on any AI failure (network, timeout, JSON parse, schema-violation, generic exception) — capture is always classified
  - `AddFlowHubAi(IConfiguration)` extension with D8 behaviour matrix: silent fallback on missing provider/key, throws on invalid `Ai__Provider`
  - `AiBootLogger` `IHostedService` writes startup log `3020 AiProviderRegistered` / `3021 AiProviderNotConfigured`
  - 18 mocked unit tests (10 for `AiClassifier`, 8 for `AddFlowHubAi`) + 4 trait-gated live integration tests
  - `Makefile`: `make test` filters `Category!=AI`; new `make test-ai` runs the live tests
- **ADR 0004**: AI integration in services (provider, abstraction, prompt + cost strategy)
- **EventId range 3000–3999** reserved for AI (extends ADR 0003 namespacing)
- **Beta MVP — Web → AI → Wallabag/Vikunja** (`docs/superpowers/specs/2026-05-04-beta-mvp-design.md`)
  - **Persistence** — `FlowHub.Persistence` becomes an active project: `FlowHubDbContext` (Sqlite) + `CaptureEntity` + `EfCaptureService` (`ICaptureService` adapter) + `AddFlowHubPersistence` extension + `MigrationRunner` IHostedService applying migrations at startup
  - **Capture record** extended with optional `Title` (set by classifier) and `ExternalRef` (set on `MarkCompletedAsync`)
  - **`Completed` terminal state** wired: `SkillRoutingConsumer` calls `ISkillIntegration.HandleAsync` after `MarkRoutedAsync`, then `MarkCompletedAsync(externalRef)` on success; throws on `!Success` to engage MassTransit retry → eventual `LifecycleFaultObserver` → `Unhandled`
  - **`ISkillIntegration` shape** simplified to one method: `Task<SkillResult> HandleAsync(Capture, CancellationToken)` (was `WriteAsync(Capture, IReadOnlyList<string>, …)`)
  - **`WallabagSkillIntegration`** — POST `/api/entries.json` with bearer auth; returns Wallabag entry id as `ExternalRef`
  - **`VikunjaSkillIntegration`** — PUT `/api/v1/projects/{id}/tasks` with bearer auth; uses classifier `Title` as task title, falls back to truncated content
  - **`AddFlowHubSkills`** — silent fallback semantics matching `AddFlowHubAi`: skill registers as no-op when `Skills:<X>:BaseUrl`/`:ApiToken`/`:DefaultProjectId` missing
  - **`SkillsBootLogger`** — `EventId 4020 SkillRegistered` / `4021 SkillNotConfigured`
  - **EventId range 4000–4999** reserved for skill startup; 2000–2999 for skill runtime; 5000–5999 for persistence
  - **UI** — `Title` column in Recent Captures grid + Capture Detail; `ExternalRef` shown in Capture Detail Metadata
  - **Tests** — 13 EF Core unit tests, 7 Wallabag + 6 Vikunja unit tests, 7 `AddFlowHubSkills` extension tests, 1 new `SkillRoutingConsumer` test for `MarkCompleted`, 2 trait-gated `[Category=BetaSmoke]` live tests
  - **Makefile** — `make test` excludes `AI` and `BetaSmoke`; new `make test-beta` runs the live Beta tests against real Wallabag + Vikunja
  - **Final code review fixups**: `CaptureDetail` now surfaces `FailureReason` for `Unhandled` captures (previous "No Skill matched" message was misleading — that case is `Orphan`); `IntegrationTestFactory.MigrationRunner` removal matches by `typeof(MigrationRunner)` instead of string-name (refactor-safe)
- **`docs/ai-usage.md`** Block-4-prep / Beta-MVP retrospective section filled in: notable adaptations (EF Core 8+ dual-provider trap, `InternalsVisibleTo` for test seeding, surgical `MigrationRunner` removal, design-time `IDesignTimeDbContextFactory`, captive-`HttpClient` flag, rate-limit recovery), generated-vs-handwritten share table (~85% AI / ~15% human in production code), reflexion sized for the submission PDF
- **ADR 0005** — Persistence (`docs/adr/0005-persistence.md`): EF Core 10 ORM, SQLite-for-Beta vs PostgreSQL-for-Block-4 provider choice, no Repository-pattern layer (`EfCaptureService` is the `ICaptureService` adapter directly), `dotnet-ef` tool manifest + in-process `MigrationRunner` Beta migrations workflow (separate init container deferred to Block 5 per 12-Factor XII), `internal sealed` entity + `InternalsVisibleTo` test visibility, keyset cursor pagination on `(CreatedAt DESC, Id DESC)` with `limit+1` probe, `IX_Captures_Stage` + `IX_Captures_CreatedAt_DESC` indexes, EventId range 5000–5999, Block-4 evolution table
- **EventId range 5000–5999** reserved for persistence runtime / startup events (5010 `LogApplyingMigrations`, 5011 `LogMigrationsApplied`)

### Changed

- `EfCaptureService` now delegates all data access to `ICaptureRepository` (no direct DbContext access)
- `EfCaptureServiceTests` updated to use NSubstitute mocks for `ICaptureRepository`
- `CaptureFilter` extended with `Tag` and `SearchTerm` optional fields
- `FlowHubDbContext.OnModelCreating` uses `ApplyConfigurationsFromAssembly` (was inline configuration)

### Removed

- `SkillRegistryStub` removed from DI (class retained for component tests)
- `IntegrationHealthServiceStub` removed from DI (class retained for component tests)
- SQLite migration `20260504120638_Initial` (replaced by PostgreSQL-compatible migrations)

### Test Results (Block 4 final)

- Total: 154 tests passing, 0 failing (verified 2026-05-06, filter: `Category!=AI&Category!=BetaSmoke`)
- Breakdown: FlowHub.Persistence.Tests 29, FlowHub.Web.ComponentTests 88, FlowHub.Api.IntegrationTests 17, FlowHub.Skills.Tests 20
- New integration tests via Testcontainers: 16 (Persistence layer)

---

## Block 5 — Deployment, Semantic Search, Production Stack (2026-05-07 → 2026-05-12)

### Added

- **Containerisation:** Multi-stage `source/FlowHub.Web/Dockerfile` and `docker/migrations/Dockerfile` (Alpine SDK → Alpine runtime, non-root `appuser`).
- **Production compose stack** (`docker-compose.yml`): six services — `flowhub.web`, `flowhub.migrations` (init container, 12-Factor XII), `postgres` (pgvector image), `rabbitmq`, `prometheus`, `grafana`. All `depends_on: service_healthy` + `service_completed_successfully` gates.
- **GitHub Actions:** `ci.yml` (build + test on every push), `release.yml` (Docker image to GHCR on `v*` tag), `migrations.yml` (efbundle artifact on migration changes).
- **ADR 0006** — Vector Search (`docs/adr/0006-vector-search.md`): pgvector `vector(1024)` column on `Captures`, HNSW index with `vector_cosine_ops`, Mistral `mistral-embed` as default embedding provider.
- **Semantic search pipeline:** `IEmbeddingService` port in Core, `AiEmbeddingService` in FlowHub.AI over `Microsoft.Extensions.AI` `IEmbeddingGenerator`. `CaptureEmbeddingConsumer` subscribes to `CaptureCreated` and populates the column asynchronously (best-effort — Capture is stored either way). `SearchEndpoints.SearchAsync` exposes `GET /api/v1/captures/search?q=…&limit=…`. `AdminEndpoints.RebuildEmbeddingsAsync` backfills captures stored before the provider was configured.
- **DemoAuthHandler** replaces `DevAuthHandler` — env-presence-driven (`Auth:OIDC:Authority`), works in any environment.
- **WireMock.Net contract test tier** (`tests/FlowHub.Skills.ContractTests/`): 13 wire-level tests against a real loopback socket for Vikunja (6) and Wallabag (7) — exercises path, bearer, JSON shape, 401 / 500 error mapping. Trait `Category=SkillContract`, wired into `make test-backend`.
- **Playwright happy-flow E2E** (`tests/FlowHub.Web.E2ETests/`): `HappyFlowTests.QuickCapture_TodoEntry_AppearsInCapturesListAndDetail` — types into AppBar QuickCapture, asserts list + detail. Trait `Category=E2E`, wrapped by `make test-e2e`.
- **`make smoke-prod`** end-to-end probe (commit `f0424ec`, 2026-05-12): boots the full stack, asserts migrations exit 0, `/health/live` 200, `/metrics` has `dotnet_*`/`http_*` series, `POST /api/v1/captures` returns 201, and `Captures.Embedding` populates within 30 s. Uses a `curlimages/curl` sidecar joined to the `flowhub.web` network namespace — no host port published. Companion `make smoke-down`.
- **`tools/FlowHub.AiPing`** console runner + `make ai-ping` / `ai-classify` / `ai-embed` targets — isolated AI smoke that reuses the production DI wiring; provider/model swappable via env.
- **`make db-ping`**: TCP probe + `SELECT 1` against the configured PostgreSQL (compose-exec → host-psql → bash `/dev/tcp` fallback chain).
- **Bruno API collection** (`bruno/`): one `.bru` per endpoint covering captures, search, retry, admin, health, metrics, openapi. `submit-capture.bru` pipes the new id into `{{captureId}}` for chained requests.
- **`make test-backend` / `test-frontend` / `test-e2e` / `test-all`** split + `playwright-install` helper.
- **`make ai-*` Passbolt integration**: `.env` can hold `passbolt://<resource-id>` refs; `SECRET_EXEC` wrapper re-sources `.env` *inside* the recipe shell and reroutes through `passbolt exec --` (when installed), so secrets never sit in plaintext on disk.
- **`docs/design/perspectives.md`** — explicit Struktur / Verhalten / Interaktion mapping (rubric "Entwurf aus verschiedenen Perspektiven").
- **`docs/insights/block-5.md`** — Block 5 reflection, test result matrix (171 tests pass across 5 projects), `make smoke-prod` transcript, and the five real defects-found-by-smoke.

### Changed

- `docs/projektbeschreibung/FlowHub_Projektbeschreibung_v4.md` §7 — disambiguated *strategic platform decisions* (renamed `ADR-001..ADR-007` → `PE-1..PE-7`) from *implementation ADRs* (`docs/adr/0001..0006`); added cross-reference index.
- `docs/design/db/er.md` — added Block-5 `Embedding vector(1024)` column + `captures_embedding_hnsw_idx` to the Captures box and a dedicated "Vector Search (Block 5)" subsection.
- `docs/spec/use-cases.md` — renumbered the colliding Block-5 use cases from UC-10 / UC-11 to UC-17 / UC-18; added explicit `Akzeptanzkriterien` blocks to UC-01..UC-11 (UC-12..UC-16 already carried inline acceptance).
- **Embedding generation is asynchronous, not sync-on-submit**: `CaptureEmbeddingConsumer` (not `EfCaptureService.SubmitAsync`) owns the Mistral call. Capture submission stays inside the NF-09 p95 < 200 ms budget regardless of embedding-provider latency; captures land in the DB even when the embedding service is down.
- `AiClassifier` and `AiEmbeddingService` configuration now treats empty-string env values as null (compose interpolation `${X:-}` substitutes empty strings, which previously bypassed the default-model fallback and crashed `flowhub.web` at startup with `OpenAI.AssertNotNullOrEmpty`).
- `docker-compose.yml` env interpolation switched from uppercase (`${EMBEDDINGS__APIKEY:-}`) to mixed-case (`${Embeddings__ApiKey:-}`) matching `.env.example` and .NET configuration keys.
- Both `Dockerfile`s now COPY `.editorconfig` into the build context so EF Core migration analyzer suppressions take effect under `TreatWarningsAsErrors`.

### Test Results (Block 5)

- Total: **171 tests passing, 0 failing** (verified 2026-05-12, filter: `Category!=AI&Category!=BetaSmoke&Category!=E2E`).
- Breakdown: FlowHub.Persistence.Tests 29, FlowHub.Web.ComponentTests 92, FlowHub.Api.IntegrationTests 17, FlowHub.Skills.Tests 20, **FlowHub.Skills.ContractTests 13 (new)**.
- New tiers introduced in Block 5: WireMock contract tests (13) and Playwright E2E (1, gated by Category=E2E).
- `make smoke-prod` green end-to-end including the embedding round-trip (~2 s p99 against Mistral).

# Block 4 Nachbereitung — Scope Design

**Date:** 2026-05-05
**Branch:** feat/beta-mvp (carry-forward; Block 4 work starts here)
**Deadline:** 2026-06-20 (Nachbereitung submission / next PVA)
**PVA:** 2026-05-23 (natural mid-point review after Slice 3)

---

## Context

Beta MVP (landed 2026-05-04) shipped the persistence foundation: `FlowHub.Persistence` project, `FlowHubDbContext` with a single `Captures` DbSet, `EfCaptureService` implementing `ICaptureService` directly against DbContext (SQLite), Initial migration, cursor pagination, ADR 0005. The Nachbereitung checklist stood at 22% (2 of ~16 rubric items earned).

Block 4 Nachbereitung completes the persistence layer: PostgreSQL, full domain model, Repository layer, dynamic queries, Testcontainers tests, Docker Compose, stub replacement, and all rubric documentation.

---

## Architecture Delta from Beta MVP

### 1. Provider switch: SQLite → PostgreSQL

`PersistenceServiceCollectionExtensions` rewired to `UseNpgsql`. All existing migrations dropped and regenerated from scratch (no SQLite data to migrate). Local dev gets Docker Compose + `make db-up` / `make db-migrate` targets.

### 2. Repository layer (new)

Repository interfaces defined in `FlowHub.Core`:

- `ICaptureRepository`
- `IChannelRepository`
- `ISkillRepository`
- `ISkillRunRepository`
- `IIntegrationRepository`
- `ITagRepository`

EF Core implementations in `FlowHub.Persistence` (one class per interface). Application-layer services (`ICaptureService`, `ISkillRegistry`, `IIntegrationHealthService`) consume repository interfaces — not DbContext directly.

`EfCaptureService` is refactored: it delegates data access to `ICaptureRepository` rather than calling `FlowHubDbContext` inline.

### 3. Full domain model

One `EntityTypeConfiguration<T>` class per entity in `FlowHub.Persistence` (replaces the existing inline `OnModelCreating` block).

### 4. Stub replacement

`ISkillRegistry` and `IIntegrationHealthService` get DB-backed implementations in `FlowHub.Persistence`, retiring the last Bogus stubs.

---

## Domain Model

### Entity catalogue

**Capture** (exists, carried forward)
```
Id           uuid PK (app-generated)
Content      text NOT NULL
Source       varchar(32) NOT NULL  -- ChannelKind enum as string; soft FK → Channel.Name
Stage        varchar(32) NOT NULL  -- LifecycleStage enum as string
CreatedAt    timestamptz NOT NULL
MatchedSkill varchar(64)           -- nullable; soft FK → Skill.Name
FailureReason text                 -- nullable
Title        varchar(512)          -- nullable; set by AiClassifier
ExternalRef  varchar(256)          -- nullable; Wallabag entry id / Vikunja task id
```
Indexes (existing): `IX_Captures_Stage`, `IX_Captures_CreatedAt_DESC`.
New index: `IX_Captures_MatchedSkill` for skill-based queries.

**Channel** (new)
```
Name         varchar(64) PK
Kind         varchar(32) NOT NULL  -- ChannelKind enum as string
IsEnabled    bool NOT NULL
Status       varchar(16) NOT NULL  -- HealthStatus enum as string
LastActiveAt timestamptz           -- nullable
```
One row per wired channel instance. No connection config column in Block 4 (deferred to Block 5).

**Skill** (new)
```
Name         varchar(64) PK
Status       varchar(16) NOT NULL  -- HealthStatus enum as string
RoutedToday  int NOT NULL DEFAULT 0
LastResetAt  timestamptz           -- nullable
```
Replaces in-memory `ISkillRegistry` stub.

**SkillRun** (new)
```
Id           uuid PK (app-generated)
SkillName    varchar(64) NOT NULL  -- FK → Skill.Name
CaptureId    uuid NOT NULL         -- FK → Capture.Id
StartedAt    timestamptz NOT NULL
CompletedAt  timestamptz           -- nullable
Success      bool NOT NULL
FailureReason text                 -- nullable
```
Audit trail for every skill routing attempt. Real DB-level FKs on both `SkillName` and `CaptureId`.

**Integration** (new)
```
Name               varchar(64) PK
Status             varchar(16) NOT NULL  -- HealthStatus enum as string
LastWriteAt        timestamptz           -- nullable
LastWriteDurationMs bigint               -- nullable; TimeSpan → milliseconds
```
Replaces in-memory `IIntegrationHealthService` stub.

**IntegrationHealthSample** (new)
```
Id              uuid PK (app-generated)
IntegrationName varchar(64) NOT NULL  -- FK → Integration.Name
SampledAt       timestamptz NOT NULL
Status          varchar(16) NOT NULL
DurationMs      bigint                -- nullable
```
Time-series health history. Index: `IX_IntegrationHealthSamples_IntegrationName_SampledAt_DESC`.

**Tag** (new — join entity)
```
CaptureId  uuid NOT NULL  -- FK → Capture.Id
Value      varchar(64) NOT NULL
PK: (CaptureId, Value) composite
```
Enables dynamic tag-based Capture filtering.

### FK strategy

| Relationship | Type | Rationale |
|---|---|---|
| Capture.Source → Channel.Name | Soft (no DB FK) | Channels can be deregistered without orphan constraint failures |
| Capture.MatchedSkill → Skill.Name | Soft (no DB FK) | Consistent with Beta MVP pattern |
| SkillRun.SkillName → Skill.Name | Hard DB FK | Controlled lifecycle; SkillRun is the audit trail |
| SkillRun.CaptureId → Capture.Id | Hard DB FK | Capture must exist before a run is recorded |
| IntegrationHealthSample.IntegrationName → Integration.Name | Hard DB FK | Samples are meaningless without their Integration |
| Tag.CaptureId → Capture.Id | Hard DB FK | Tags are owned by Capture |

---

## Slice Breakdown

### Slice 1 — Foundation + Spec docs (May 5–10)

**Technical:**
- PostgreSQL switch: `UseNpgsql`, remove SQLite packages, update `appsettings.Development.json`
- Docker Compose snippet for PostgreSQL + `make db-up` / `make db-migrate` Makefile targets
- Drop existing migrations; regenerate `0001_Initial` for Capture-only schema
- Extract `CaptureEntityTypeConfiguration : IEntityTypeConfiguration<CaptureEntity>`
- `ICaptureRepository` in `FlowHub.Core` with methods: `AddAsync`, `GetByIdAsync`, `ListAsync` (cursor), `UpdateAsync`
- `EfCaptureRepository` in `FlowHub.Persistence` implementing `ICaptureRepository`
- Refactor `EfCaptureService` to delegate to `ICaptureRepository`
- DI wiring updated in `AddFlowHubPersistence`

**Docs:**
- Use Cases: Capture CRUD, lifecycle filter, tag filter, content search (ILIKE / `Contains` — not pg full-text, deferred to Block 5), Skill-run history query, Integration health history query
- NfA SMART: query latency targets (p95 < 100ms for list), index requirements, migration strategy (zero-downtime via `--idempotent` scripts), data volume assumptions
- Solution Vision update: add persistence layer paragraph (PostgreSQL, EF Core, Repository pattern, Migrations-First)

**Rubric items targeted:** Use Cases (5), NfA (5), Solution Vision (5), Code strukturiert (partial 7), Source in Git (2)

---

### Slice 2 — Channel + Skill (May 10–18)

**Technical:**
- `ChannelEntity` + `ChannelEntityTypeConfiguration`
- `SkillEntity` + `SkillEntityTypeConfiguration`
- `IChannelRepository` + `EfChannelRepository`
- `ISkillRepository` + `EfSkillRepository`
- DB-backed `ISkillRegistry` implementation (replaces Bogus stub)
- Migration `0002_AddChannelAndSkill`
- Dashboard channel list wired to `IChannelRepository`
- ai-usage.md paragraph: AI involvement in entity design and configuration class generation

**Rubric items targeted:** Intelligente Services (partial 6), Code strukturiert (partial 7)

---

### Slice 3 — Integration + Tag + SkillRun + IntegrationHealthSample (May 18–25)

**Technical:**
- `IntegrationEntity`, `IntegrationHealthSampleEntity`, `SkillRunEntity`, `TagEntity` (join)
- `EntityTypeConfiguration<T>` for each
- `IIntegrationRepository` + `EfIntegrationRepository`
- DB-backed `IIntegrationHealthService` (replaces last Bogus stub)
- Dynamic LINQ Capture filter: `Expression<Func<CaptureEntity, bool>>` for Stage, ChannelName, Tag, search term (combined via `AndAlso`)
- N+1 prevention: `Include` / projection for SkillRun → Capture hot path
- Migration `0003_Block4FullDomain` (single migration for all remaining entities)
- ER diagram updated to full schema in `docs/design/db/er.md` (Mermaid)
- ai-usage.md paragraph: AI involvement in expression-tree filter and N+1 avoidance

**Rubric items targeted:** DB-Modell (3), Intelligente Services (6 complete), Code strukturiert (7 complete), Erkenntnisse (partial 3)

---

### Slice 4 — Tests + Docker (May 25–Jun 10)

**Technical:**
- Testcontainers PostgreSQL: repository tests per aggregate root (happy path, empty result, edge cases)
- Integration tests for API endpoints against real DB (extend existing `IntegrationTestFactory`, switch from InMemory to Testcontainers)
- Migrations smoketest: apply up → down → up on empty DB container
- Docker Compose: full stack profile (FlowHub.Web + PostgreSQL), migrations as separate init service (12-Factor XII prep)
- `make db-up` / `make db-migrate` exercised against Docker Compose DB
- Acceptance criteria per Use Case documented (data flows, edge cases: empty result, concurrent write, migration rollback)
- `docs/test-strategy.md` updated: Testcontainers rationale, provider-parity argument, test pyramid for persistence layer
- Test results in `CHANGELOG [Unreleased]`: test count, pass rate, coverage delta

**Rubric items targeted:** Abnahmekriterien (5), Test-Strategie (5), Unit-Tests (3), Test-Ergebnisse (3), Sub-Systeme als Container (5)

---

### Slice 5 — Rubric docs (Jun 10–20)

**Docs:**
- `docs/insights/block-4.md`: full AI-usage narrative assembled from Slice 1–4 rolling notes
- Struktur/Verhalten/Interaktion diagrams: class diagram (structure), sequence diagrams for Capture intake + Skill routing hot paths (behaviour), call-chain diagram Web→Service→Repository→DbContext→DB (interaction)
- KI-Reflexion / Fazit: AI strengths (boilerplate, migration generation, expression-tree scaffolding) and weaknesses (schema anticipation, performance blindness, N+1 unawareness)
- `docs/ai-usage.md` Block 4 section finalised (12-pt item)
- Nachbereitung checklist final tick-through in `vault/Blöcke/04 Persitence/04 Persitence - c) Nachbereitung.md`
- `CHANGELOG [Unreleased]` complete

**Rubric items targeted:** KI-Werkzeug-Nutzung (12), KI-Reflexion (7), Struktur/Verhalten/Interaktion (7), Erkenntnisse (3 complete)

---

## Rubric Coverage Map

| Rubric item | Max pts | Slice | Status |
|---|---:|---|---|
| Use Cases | 5 | 1 | — |
| NfA SMART | 5 | 1 | — |
| Solution Vision | 5 | 1 | — |
| Lösungsansatz & Architektur | 7 | — | **done** (ADR 0005) |
| Struktur/Verhalten/Interaktion | 7 | 5 | — |
| DB-Modell | 3 | 3 | — |
| Code strukturiert | 7 | 1–3 | — |
| Erkenntnisse dokumentiert | 3 | rolling | partial done |
| Source in Git | 2 | 1 | — |
| Abnahmekriterien | 5 | 4 | — |
| Test-Strategie | 5 | 4 | — |
| Unit-Tests | 3 | 4 | — |
| Test-Ergebnisse | 3 | 4 | — |
| KI-Werkzeug-Nutzung (12 pts ⭐) | 12 | 5 | — |
| Intelligente Services | 6 | 2–3 | — |
| Sub-Systeme als Container | 5 | 4 | — |
| KI-Reflexion | 7 | 5 | — |
| **Quarkus / Jakarta EE** | 10 | N/A | **skipped** (.NET stack) |
| **Effective total** | **90** | | |

---

## Out of Scope (Block 4)

- Connection config column on `Channel` → Block 5
- Read-replicas / sharding → not in course scope
- Vector/embedding search → Block 5
- Production backup tooling → Block 5
- OIDC / Authentik auth → Block 5
- Outbox / idempotency receiver → explicitly deferred (ADR 0003)

# Block 4 Erkenntnisse — Persistence Layer

## What Was Built

Block 4 completed the persistence layer for FlowHub:

- **PostgreSQL switch**: Replaced SQLite with PostgreSQL 17 + Npgsql EF Core provider. No data to migrate; existing SQLite migration dropped and regenerated.
- **Repository pattern**: `ICaptureRepository`, `IChannelRepository`, `ISkillRepository`, `IIntegrationRepository`, `ITagRepository`, `ISkillRunRepository` — all in `FlowHub.Core` returning domain types. EF Core implementations in `FlowHub.Persistence`.
- **Full domain model**: 6 new entities (Channel, Skill, SkillRun, Integration, IntegrationHealthSample, Tag) with real DB FKs where appropriate (hard FKs on audit trail entities, soft FKs for deregisterable references).
- **Stub retirement**: `EfSkillRegistry` replaced `SkillRegistryStub`; `EfIntegrationHealthService` replaced `IntegrationHealthServiceStub` in DI.
- **Dynamic filter**: `CaptureQueryBuilder` combines Stage, Source, Tag, and SearchTerm (ILike) predicates via expression composition.
- **Testcontainers**: 16 integration tests against real PostgreSQL (provider-parity with production).
- **Docker Compose**: Full stack with postgres service + migrations init container (12-Factor XII).

## AI Usage by Slice

### Slice 1: PostgreSQL + Repository Foundation

AI generated the full `EfCaptureRepository` from the interface contract. The initial output used `context.Captures.FindAsync(id)` (tracking) instead of `.AsNoTracking().FirstOrDefaultAsync()` — human corrected for read operations. AI drafted `CaptureEntityTypeConfiguration`; human added `IX_Captures_MatchedSkill` index (missed from initial AI output).

The repository interface design (`ICaptureRepository` returning domain types, not entities) was a human architectural decision. AI implemented the resulting design quickly once the contract was defined.

**Estimated AI share:** ~90% implementation code, ~10% interface and architectural decisions.

### Slice 2: Channel + Skill Entities

AI produced `ChannelEntity`, `SkillEntity`, their type configurations, and the repositories in one pass. Field lengths were wrong on first attempt (AI used varchar(128) for Name; spec says varchar(64)) — fixed in review. `EfSkillRegistry` wrapping `ISkillRepository` (two-line class) was a human-driven design decision to keep the driving port decoupled from the driven port.

**Estimated AI share:** ~88% code, ~12% review corrections.

### Slice 3: Full Domain Model + Dynamic Filter

AI generated 4 entity classes + configurations + repositories. Main corrections:
- FK cascade rules: AI defaulted all FKs to CASCADE DELETE. Human changed SkillRun→Skill to `Restrict` (preserve audit trail if skill name is updated/removed); Capture→Channel and Capture→Skill are soft FKs (no DB FK at all).
- `CaptureQueryBuilder` was AI-generated. Human noticed the N+1 on Tags (EF does not auto-load nav properties) and added `.Include(c => c.Tags)`.
- AI placed the `.Include()` call after `.AsNoTracking()` — correct order in EF Core 10; the cursor ordering precondition comment was a human addition after code review.

**Estimated AI share:** ~85% code, ~15% architectural decisions and corrections.

### Slice 4: Tests + Docker

`PostgresFixture` with per-test isolated databases was AI-scaffolded; human reviewed `NpgsqlConnectionStringBuilder` usage and the `CREATE DATABASE` admin connection pattern. All 16 test methods were AI-generated from the method names and expected behaviors.

Docker Compose `flowhub.migrations` service (12-Factor XII) was a human design decision; AI filled in the YAML after the pattern was described.

**Estimated AI share:** ~92% test code, ~85% Docker YAML.

## AI Usage Metrics (Block 4)

| Artifact | Total Lines | AI-Generated | Human Lines | AI % |
|---|---:|---:|---:|---:|
| Entity classes (7) | ~120 | ~108 | ~12 | 90% |
| EntityTypeConfiguration (7) | ~130 | ~117 | ~13 | 90% |
| Repository impls (6) | ~350 | ~315 | ~35 | 90% |
| Service impls (EfSkillRegistry, EfIntegrationHealthService) | ~30 | ~27 | ~3 | 90% |
| CaptureQueryBuilder | ~30 | ~25 | ~5 | 83% |
| EfCaptureService refactor | ~70 | ~60 | ~10 | 86% |
| Test files (4 classes, 16 tests) | ~230 | ~210 | ~20 | 91% |
| Docker Compose | ~50 | ~40 | ~10 | 80% |
| **Total** | **~1010** | **~902** | **~108** | **~89%** |

Human contributions were concentrated in: FK strategy decisions, N+1 detection, field length corrections, index additions, and Docker Compose service dependency pattern.

## KI-Reflexion / Fazit

### Stärken der KI-Unterstützung (Strengths)

**Boilerplate-Generierung:** Die sieben `IEntityTypeConfiguration<T>`-Klassen (insgesamt ~130 Zeilen) wurden vollständig von der KI generiert. Ohne KI wäre dieser Schritt zeitintensiv und fehleranfällig gewesen — die Klassen sind strukturell identisch, unterscheiden sich nur in Tabellennamen und Feldlängen.

**Migrationsgenerierung:** Das Scaffolding der drei EF Core Migrations (0001–0003) war vollständig automatisiert. Die KI hat den Design-Time-Factory-Ansatz korrekt angewendet.

**Expression-Tree-Filter:** `CaptureQueryBuilder` mit kombinierter Prädikatenkomposition (Stage, Source, Tag, ILike) war ohne KI-Unterstützung deutlich aufwendiger. Die KI hat das korrekte Muster für EF Core LINQ-Komposition auf Anhieb angewendet.

**Test-Scaffolding:** 16 Integrationstests wurden von der KI generiert und bestehen alle beim ersten Durchlauf. Die Teststruktur (Arrange/Act/Assert, FluentAssertions-Syntax) ist konsistent und entspricht den Projektvorgaben.

### Schwächen und Grenzen (Weaknesses)

**N+1-Blindheit:** Die KI hat `EfCaptureRepository.ListAsync` initial ohne `.Include(c => c.Tags)` generiert. Das N+1-Problem wäre in einer Codeüberprüfung aufgefallen — KI hat keine implizite Performance-Awareness für Navigation Properties.

**FK-Strategie:** Die KI hat durchgängig Hard-FKs mit CASCADE DELETE vorgeschlagen, ohne die Unterscheidung zwischen "owned" Entitäten (Tags, IntegrationHealthSamples) und "referenced" Entitäten (Channel, Skill) zu erkennen. Die Soft-FK-Entscheidung für Capture→Channel und Capture→Skill war eine Domain-Entscheidung, die domänenspezifisches Verständnis erforderte.

**Feldlängen:** Erste Generierung verwendete varchar(128) für Name-Felder. Die spec schreibt varchar(64) vor. KI liest Spezifikationen korrekt, wenn sie explizit zitiert werden — aber ohne direkten Verweis wird auf "sichere" Standardwerte ausgewichen.

**Paket-Versionen:** Der Plan hatte Npgsql 9.0.4 spezifiziert (EF Core 9 Ära). Der implementierende Subagent musste die Version auf 10.0.1 anpassen. Plan-Templates mit versionsgepinnten Paketen degradieren mit der Zeit.

### Fazit

KI-Unterstützung hat in Block 4 die Implementierungszeit für Persistence-Infrastruktur auf ~89% des Codes reduziert. Der menschliche Beitrag war konzentriert auf: Architekturentscheidungen (FK-Strategie, Repository-Interface-Design), Performance-Korrekturen (N+1), Spezifikationsabgleich (Feldlängen, Indexe) und Code Review.

Die Kombination aus Brainstorming-Skill → Spec-Dokument → Plan-Dokument → Subagent-getriebene Implementierung hat sich bewährt: Jede Phase hat die nächste informiert, ohne dass die KI unkontrolliert in die falsche Richtung implementiert hat.

**Einschätzung:** KI ist ein starker Accelerator für Infrastruktur-Code (Boilerplate, Migrations, Tests), erfordert aber menschliche Führung bei Architektur- und Domain-Entscheidungen.

## Test Results (as of 2026-05-18)

`dotnet test FlowHub.slnx --filter "FullyQualifiedName!~E2ETests"` — **223 tests pass, 0 fail, 6 skipped** (live-service integration tests requiring external API keys):

| Project | Passed | Skipped | Duration | Coverage |
|---|--:|--:|--:|---|
| `FlowHub.Persistence.Tests` | 29 | 0 | ~24 s | EF Core + Testcontainers PostgreSQL — `EfCaptureRepository`, `EfCaptureService`, `EfSkillRegistry`, `EfIntegrationHealthService`, migration smoke test |
| `FlowHub.Web.ComponentTests` | 144 | 0 | ~11 s | bUnit — every Razor page + MassTransit harness pipeline tests + classifier/dispatcher/enricher unit tests |
| `FlowHub.Api.IntegrationTests` | 17 | 0 | ~11 s | `WebApplicationFactory` + Testcontainers — full HTTP pipeline against real Postgres |
| `FlowHub.Skills.Tests` | 20 | 0 | ~1 s | Unit-level handler tests for Vikunja + Wallabag adapters |
| `FlowHub.Skills.ContractTests` | 13 | 0 | ~1 s | WireMock.Net wire-contract tests on a real loopback socket |
| `FlowHub.Skills.IntegrationTests` | 0 | 2 | — | `just test-beta` — needs live Wallabag + Vikunja |
| `FlowHub.AI.IntegrationTests` | 0 | 4 | — | `just test-ai` — needs `Ai__*__ApiKey` |
| **Total** | **223** | **6** | | |

E2E tests (`FlowHub.Web.E2ETests`, Playwright) are run on demand via `just test-e2e` and gated by the Web server + Chromium dependency. Latest green CI run: <https://github.com/freaxnx01/FlowHub-CAS-AISE/actions/workflows/ci.yml>.

### Persistence-Layer Coverage

The 29 `FlowHub.Persistence.Tests` cover the full persistence surface introduced in this block:

| Test class | What it verifies | Tooling |
|---|---|---|
| `EfCaptureRepositoryTests` | Create/Read/Update/Delete; cursor pagination ordering (`CreatedAt DESC, Id DESC`); filter composition (Stage / Source / Tag / SearchTerm); `.Include(Tags)` round-trip | Testcontainers PostgreSQL 17 |
| `EfCaptureServiceTests` | Stage transitions: `Raw → Classified → Routed → Completed`; `MarkClassifiedAsync` now also persists `VikunjaProject`; `MarkOrphan`, `MarkUnhandled` paths | Testcontainers + `IPublishEndpoint` substitute |
| `EfSkillRegistryTests` | Skill registry CRUD; deregistration; lookup-by-name | Testcontainers |
| `EfIntegrationHealthServiceTests` | Health-sample insertion; latest-per-integration query | Testcontainers |
| `MigrationSmokeTest` | All migrations from `0001_Initial` to `0008_AddVikunjaProjectToCapture` apply cleanly on an empty database | Testcontainers — bare `db.Database.MigrateAsync()` |

The cursor-pagination edge cases (limit+1 probe, ordering precondition) and the FK-strategy choices documented in the AI-Usage section above are all covered by these 29 tests against real Postgres — no in-memory provider drift.

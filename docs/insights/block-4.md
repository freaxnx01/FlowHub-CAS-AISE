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

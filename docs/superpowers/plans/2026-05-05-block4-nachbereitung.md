# Block 4 Nachbereitung Implementation Plan — Index

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Complete the FlowHub persistence layer for CAS AISE Block 4 Nachbereitung: switch to PostgreSQL, add Repository pattern, extend domain model with 6 new entities, replace Bogus stubs with DB-backed services, add Testcontainers integration tests, Docker Compose full stack, and all rubric documentation.

**Architecture:** EF Core 10 + Npgsql replaces SQLite. Repository interfaces live in `FlowHub.Core` (returning domain types), implementations in `FlowHub.Persistence`. `EfCaptureService` delegates all data access to `ICaptureRepository`. New `EfSkillRegistry` and `EfIntegrationHealthService` retire the last two Bogus stubs.

**Tech Stack:** EF Core 10, Npgsql.EntityFrameworkCore.PostgreSQL (9.0.4 — if build errors on EF Core 10 compat, run `dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL` and use the resolved version), Testcontainers.PostgreSql 3.10.0, xunit 2.9.3, FluentAssertions 6.12.2, NSubstitute 5.3.0, Docker Compose, MudBlazor 8.5.1

**Deadline:** 2026-06-20 (Nachbereitung submission). PVA mid-point: 2026-05-23 (after Slice 3).

---

## Slice Plans (execute in order — each slice depends on the previous)

| Slice | File | Dates | Tasks |
|---|---|---|---|
| 1 — Foundation | [slice1-foundation](./2026-05-05-block4-slice1-foundation.md) | May 5–10 | 1–4: PostgreSQL switch, Repository pattern, spec docs |
| 2 — Channel + Skill | [slice2-channel-skill](./2026-05-05-block4-slice2-channel-skill.md) | May 10–18 | 5–7: Channel/Skill entities, EfSkillRegistry, migration 0002 |
| 3 — Full Domain | [slice3-full-domain](./2026-05-05-block4-slice3-full-domain.md) | May 18–25 | 8–12: Integration, Tag, SkillRun, CaptureQueryBuilder, migration 0003 |
| 4 — Tests + Docker | [slice4-tests-docker](./2026-05-05-block4-slice4-tests-docker.md) | May 25–Jun 10 | 13–17: Testcontainers, Docker Compose, testing docs |
| 5 — Rubric Docs | [slice5-docs](./2026-05-05-block4-slice5-docs.md) | Jun 10–20 | 18–21: ai-usage, diagrams, KI-Reflexion, vault checklist |

---

## Complete File Map

### Created

```
source/FlowHub.Core/Captures/ICaptureRepository.cs
source/FlowHub.Core/Captures/SkillRun.cs
source/FlowHub.Core/Captures/ITagRepository.cs
source/FlowHub.Core/Captures/ISkillRunRepository.cs
source/FlowHub.Core/Channels/Channel.cs
source/FlowHub.Core/Channels/IChannelRepository.cs
source/FlowHub.Core/Health/ISkillRepository.cs
source/FlowHub.Core/Health/IIntegrationRepository.cs
source/FlowHub.Persistence/Repositories/EfCaptureRepository.cs
source/FlowHub.Persistence/Repositories/EfChannelRepository.cs
source/FlowHub.Persistence/Repositories/EfSkillRepository.cs
source/FlowHub.Persistence/Repositories/EfIntegrationRepository.cs
source/FlowHub.Persistence/Repositories/EfTagRepository.cs
source/FlowHub.Persistence/Repositories/EfSkillRunRepository.cs
source/FlowHub.Persistence/Entities/CaptureEntityTypeConfiguration.cs
source/FlowHub.Persistence/Entities/ChannelEntity.cs
source/FlowHub.Persistence/Entities/ChannelEntityTypeConfiguration.cs
source/FlowHub.Persistence/Entities/SkillEntity.cs
source/FlowHub.Persistence/Entities/SkillEntityTypeConfiguration.cs
source/FlowHub.Persistence/Entities/IntegrationEntity.cs
source/FlowHub.Persistence/Entities/IntegrationEntityTypeConfiguration.cs
source/FlowHub.Persistence/Entities/IntegrationHealthSampleEntity.cs
source/FlowHub.Persistence/Entities/IntegrationHealthSampleEntityTypeConfiguration.cs
source/FlowHub.Persistence/Entities/SkillRunEntity.cs
source/FlowHub.Persistence/Entities/SkillRunEntityTypeConfiguration.cs
source/FlowHub.Persistence/Entities/TagEntity.cs
source/FlowHub.Persistence/Entities/TagEntityTypeConfiguration.cs
source/FlowHub.Persistence/EfSkillRegistry.cs
source/FlowHub.Persistence/EfIntegrationHealthService.cs
source/FlowHub.Persistence/CaptureQueryBuilder.cs
tests/FlowHub.Persistence.Tests/Fixtures/PostgresFixture.cs
tests/FlowHub.Persistence.Tests/Repositories/EfCaptureRepositoryTests.cs
tests/FlowHub.Persistence.Tests/Repositories/EfSkillRegistryTests.cs
tests/FlowHub.Persistence.Tests/Repositories/EfIntegrationHealthServiceTests.cs
tests/FlowHub.Persistence.Tests/Migrations/MigrationSmokeTest.cs
docs/spec/nfa.md
docs/design/db/er.md
docs/design/structure/class-diagram.md
docs/design/sequences/capture-intake.md
docs/design/sequences/skill-routing-hot-path.md
docs/insights/block-4.md
```

### Modified

```
Directory.Packages.props                            — add Npgsql + Testcontainers packages
source/FlowHub.Persistence/FlowHub.Persistence.csproj — SQLite → Npgsql
source/FlowHub.Persistence/PersistenceServiceCollectionExtensions.cs — UseNpgsql, new DI
source/FlowHub.Persistence/FlowHubDbContext.cs      — ApplyConfigurationsFromAssembly, new DbSets
source/FlowHub.Persistence/FlowHubDbContextFactory.cs — UseNpgsql
source/FlowHub.Persistence/EfCaptureService.cs     — delegate to ICaptureRepository
source/FlowHub.Core/Captures/CaptureFilter.cs      — add Tag, SearchTerm fields (Slice 3)
source/FlowHub.Persistence/Entities/CaptureEntity.cs — add Tags nav (Slice 3)
source/FlowHub.Web/Program.cs                      — swap stubs for EF implementations
docker-compose.yml                                  — add postgres + migrations services
Makefile                                            — add db-up, db-migrate targets
tests/FlowHub.Persistence.Tests/FlowHub.Persistence.Tests.csproj — Testcontainers packages
tests/FlowHub.Persistence.Tests/EfCaptureServiceTests.cs — update for ICaptureRepository
docs/spec/use-cases.md                              — add persistence use cases
docs/spec/system-context.md                         — Solution Vision paragraph
docs/spec/testing-strategy.md                       — Testcontainers section
docs/ai-usage.md                                    — Block 4 section
vault/Blöcke/04 Persitence/04 Persitence - c) Nachbereitung.md — tick rubric items
CHANGELOG.md                                        — Unreleased entries
```

### Deleted / Regenerated (migrations)

```
source/FlowHub.Persistence/Migrations/20260504120638_Initial.cs      — delete (SQLite)
source/FlowHub.Persistence/Migrations/20260504120638_Initial.Designer.cs — delete
source/FlowHub.Persistence/Migrations/FlowHubDbContextModelSnapshot.cs  — regenerated
→ New: 0001_Initial (PostgreSQL), 0002_AddChannelAndSkill, 0003_Block4FullDomain
```

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
| KI-Werkzeug-Nutzung (⭐ 12 pts) | 12 | 5 | — |
| Intelligente Services | 6 | 2–3 | — |
| Sub-Systeme als Container | 5 | 4 | — |
| KI-Reflexion | 7 | 5 | — |
| **Quarkus / Jakarta EE** | 10 | N/A | **skipped** (.NET stack — note in PDF) |
| **Effective total** | **90** | | |

---

## Code Quality Rules (applies to all slices)

- No comments unless the WHY is non-obvious
- All async methods take `CancellationToken cancellationToken = default`
- Use `AsNoTracking()` for all read queries
- Use `nameof(LifecycleStage.Raw)` style — never string literals for enum comparisons
- Guard clauses at top of methods, no nested if/else pyramids
- `internal sealed class` for all Persistence implementations
- `services.AddScoped<IFoo, EfFoo>()` for all DI registrations

# ADR 0005 — Persistence: ORM, Provider, Repository Pattern, Migrations Workflow

- **Status:** Accepted
- **Date:** 2026-05-04
- **Block:** Block 4 (Persistence) — landed early via Beta MVP slice
- **Decider:** freax
- **Affects:** `source/FlowHub.Persistence/`, `source/FlowHub.Web/Program.cs`, `tests/FlowHub.Persistence.Tests/`, `tests/FlowHub.Api.IntegrationTests/IntegrationTestFactory.cs`, `Directory.Packages.props`, `.config/dotnet-tools.json`

---

## Context

Block 2 shipped `CaptureServiceStub` — a Bogus-backed in-memory `ICaptureService`
implementation that survives only the lifetime of one `make run`. Block 4's Moodle
Auftrag asks for "ein geeignetes Datenmodell entwerfen und über ORM-Abstraktion
realisieren ... mit dynamischen Abfragen" — i.e., real persistence, with an ORM,
backing the same driving port the UI already consumes.

The Beta MVP slice (`docs/superpowers/specs/2026-05-04-beta-mvp-design.md`,
`docs/superpowers/plans/2026-05-04-beta-mvp.md`) landed a vertical end-to-end demo
*before* Block 4 formally begins (2026-05-09). The Beta slice's primary purpose was
to validate the architecture against the real homelab Wallabag/Vikunja, not to
deliver the full Block-4 scope. That shaped several decisions in this ADR — they
make sense for the Beta-scope demo and are explicit about which evolutions Block 4
will re-litigate.

The Quarkus / Hibernate / Panache / Jakarta-Data programming criterion remains
**N/A** for FlowHub's .NET stack — same precedent as ADRs 0002, 0003, 0004. The
course's nominal Hibernate / Panache / Jakarta Data vocabulary is the teacher's
reference stack; FlowHub's .NET-native equivalents (EF Core 10, `DbSet<T>` + LINQ,
`IQueryable<T>` + Expression Trees) are presented in their own terms and the
mapping is recorded in `vault/Blöcke/04 Persitence/04 Persitence - c) Nachbereitung.md`'s
intro.

---

## Decision

### 1. ORM = `Microsoft.EntityFrameworkCore` 10.0.7

Single ORM, .NET-native, first-party. No Dapper (we don't need raw-SQL micro-ORM
performance for FlowHub's ingest-and-classify load profile), no NHibernate (not
.NET-native enough), no LINQ-to-DB (smaller community, fewer providers). EF Core
10 is the latest stable, ships with .NET 10 SDK, and matches the toolchain the rest
of the codebase already pins (`global.json` 10.0.x).

The hexagonal seam stays unchanged: `ICaptureService` (driving port, in
`FlowHub.Core`) is the only contract the Web/API layers know about; `EfCaptureService`
(driven adapter, in `FlowHub.Persistence`) is the implementation. EF Core's
`DbContext` is **not** exposed beyond the persistence assembly.

### 2. Provider = SQLite for the Beta, PostgreSQL for Block 4 production

The Beta MVP runs on SQLite (`Microsoft.EntityFrameworkCore.Sqlite` 10.0.7).
Connection-string fallback is `Data Source=flowhub.db` (relative to working dir,
which is `source/FlowHub.Web/` under `make run`). This is a deliberate scope
decision:

- Zero infrastructure setup — no Docker container, no `make db-up`, no port
  conflicts. The user can `make run` and have a working app immediately.
- The EF Core API surface is provider-agnostic for the Beta's query shapes (no
  Postgres-specific operators, no `JSONB` columns, no full-text search yet). All
  13 unit tests in `FlowHub.Persistence.Tests` run against `Microsoft.EntityFrameworkCore.InMemory`,
  which makes provider-specific behaviour invisible at the test layer — that's
  intentional for the Beta and will need to evolve in Block 4 (see §10).
- Block 4 swaps to PostgreSQL via `UseNpgsql` + `ConnectionStrings:Default` env
  var, with Testcontainers PostgreSQL replacing `InMemory` for repository tests
  (per the spec's "Testcontainers PostgreSQL bevorzugt vor SQLite-In-Memory wegen
  Provider-Quirks" guidance). Migration source code regenerates from
  `OnModelCreating` against the new provider — schema is identical except for
  column-type discriminators that EF handles transparently.

The asymmetry — SQLite-in-prod (Beta) vs PostgreSQL-in-prod (Block 4) — is
documented here so it doesn't feel like a regression when Block 4 changes the
default.

### 3. No Repository pattern layer; `EfCaptureService` is the `ICaptureService` adapter

The Block 4 Moodle Auftrag mentions "Repositories" as one of the standard ORM
patterns. FlowHub deliberately does NOT introduce a separate repository interface
between `ICaptureService` and `DbContext`. Reasons:

- `DbSet<T>` already *is* a repository in EF Core's design (a `Queryable<T>` over
  one table with `Add`, `Remove`, `Update` semantics). Wrapping it in an
  `ICaptureRepository` would be a level of ceremony with no testability or
  abstraction value the existing `ICaptureService` doesn't already provide.
- The existing `ICaptureService` *is* the repository abstraction in a hexagonal
  sense: it's the driving port the application code consumes; whether the
  implementation talks to EF Core or to Bogus or to some future read-model is
  transparent to callers.
- The spec's "Repository-Pattern-Entscheid" rubric checkbox is fulfilled by this
  decision being recorded — repository-as-separate-layer is a common but
  not-mandatory pattern; choosing not to use it is a defensible architectural
  call.

If a future driven adapter needs the `DbContext` directly (e.g. a read-model
projection or a complex multi-aggregate transaction that doesn't fit the
`ICaptureService` shape), it lives in `FlowHub.Persistence` and gets its own
driving port. We're not introducing a generic `IRepository<T>` — that's
abstraction without addition.

### 4. Migrations workflow = `dotnet-ef` tool manifest + in-process `MigrationRunner` (Beta) → separate init container (Block 5)

The repo pins `dotnet-ef` via `.config/dotnet-tools.json` (created during the Beta
slice). This means:

- `dotnet tool restore` after a fresh clone gives every developer the same
  EF version locally.
- `dotnet ef migrations add <Name> --project source/FlowHub.Persistence
  --startup-project source/FlowHub.Web --output-dir Migrations` is the canonical
  add-migration command.
- A design-time `IDesignTimeDbContextFactory<FlowHubDbContext>` (in
  `source/FlowHub.Persistence/FlowHubDbContextFactory.cs`, internal sealed) lets
  the EF tooling discover the context without booting the full host.

For migration *application*, the Beta uses a `MigrationRunner` `IHostedService`
that calls `db.Database.MigrateAsync()` at app startup (EventIds 5010/5011). This
is **convenient for `make run`** but is explicitly out-of-line with **12-Factor
XII** ("Run admin/management tasks as one-off processes"). Block 5 will:

- Move migration application to a separate init container in the production
  Compose / K8s manifests.
- Generate migration SQL bundles (`dotnet ef migrations script`) for ops review
  before applying.
- Keep `MigrationRunner` in `Development` only (or remove entirely — TBD with the
  Block 5 deployment spec).

The Beta's in-process migration runner is acceptable scope debt: the demo path
needs migrations to exist on first run, and the deployment-grade alternative
requires Compose orchestration that isn't built yet.

### 5. Entity visibility = `internal sealed` + `InternalsVisibleTo`

`CaptureEntity` (and any future entity) lives at `internal sealed` visibility in
`source/FlowHub.Persistence/Entities/`. `FlowHubDbContext.Captures` is
`internal DbSet<CaptureEntity>`. Tests reach the entity via
`[assembly: InternalsVisibleTo(...)]` in `source/FlowHub.Persistence/Properties/AssemblyInfo.cs`
— currently exposing internals to:

- `FlowHub.Persistence.Tests` — for direct entity instantiation in unit-test
  seeding and `db.Captures.AddRange(...)` setup.
- `FlowHub.Api.IntegrationTests` — for `IntegrationTestFactory` deterministic
  seeding (10 rows: 2 Orphan, 2 Completed, 2 Raw, 2 Classified, 1 Routed, 1
  Unhandled). Seeding through `ICaptureService.SubmitAsync` would publish
  `CaptureCreated` events into the test's MassTransit harness, causing
  non-deterministic state at test start.

The internal-visibility surface area is now two test assemblies. This is
acceptable trade-off for keeping the entity off the public surface (preserves
`ICaptureService` as the only API surface). Block 4's domain expansion (Skill,
SkillRun, Channel, Integration, IntegrationHealthSample, Tag) follows the same
pattern.

### 6. Cursor pagination = keyset on `(CreatedAt DESC, Id DESC)` with `limit+1` probe

`EfCaptureService.ListAsync` implements offset-free pagination:

```
WHERE c.CreatedAt < cursor.CreatedAt
   OR (c.CreatedAt == cursor.CreatedAt AND c.Id < cursor.Id)
ORDER BY c.CreatedAt DESC, c.Id DESC
LIMIT @limit + 1
```

The `+1` probe row determines whether a `next` cursor should be issued. The
`(CreatedAt, Id)` composite handles ties on identical timestamps (which Bogus
seeding can produce). Cursor format is hand-rolled URL-safe base64 of JSON
`(CreatedAt, Id)` tuples — the same format Slice A introduced for the REST API.
No EF-Core-specific feature is used; the same pattern works against PostgreSQL
unchanged in Block 4.

`Limit` is clamped server-side to `[1, 200]` (`Math.Clamp`); the spec calls out
200 as the max.

### 7. Indexes = `IX_Captures_Stage` (filtered list, "Needs Attention") + `IX_Captures_CreatedAt_DESC` (Recent Captures + pagination)

Two non-unique indexes are declared in `FlowHubDbContext.OnModelCreating`:

- **`IX_Captures_Stage`** — drives the Dashboard "Needs Attention" count
  (`Stage IN ('Orphan', 'Unhandled')`) and the lifecycle filter chips on
  `/captures`.
- **`IX_Captures_CreatedAt_DESC`** — drives Recent Captures (top-N descending)
  and cursor pagination on `/captures`. Declared as `IsDescending()` so SQLite /
  PostgreSQL pick the right scan direction.

No index on `MatchedSkill` yet — the matched-skill filter is not exposed in the
UI. Block 4 will add `(Source, CreatedAt DESC)` once the channel filter chips
land in the spec.

### 8. EntityTypeConfiguration = inline `OnModelCreating` (Beta) → per-entity classes (Block 4)

The Beta has one entity (`CaptureEntity`); inline configuration in
`OnModelCreating` is the pragmatic choice. When Block 4 adds Skill / SkillRun /
Channel / Integration / IntegrationHealthSample / Tag / CaptureTag(join), each
will get its own `IEntityTypeConfiguration<T>` class in
`source/FlowHub.Persistence/Configurations/` and `OnModelCreating` will reduce
to `modelBuilder.ApplyConfigurationsFromAssembly(typeof(FlowHubDbContext).Assembly)`.
The refactor is mechanical — no public-API change.

### 9. AI audit fields on `Capture` = deferred to Block 4

ADR 0004 §"Consequences for next blocks" promised AI audit fields
(`provider`, `model`, `duration_ms`, `was_fallback`) per classification. The
Beta's `CaptureEntity` does NOT carry them yet — those columns are part of
Block 4 scope when the Capture-CRUD use cases formalize what audit data
is queryable. The schema migration in Block 4 will add the columns; existing
rows in the homelab SQLite DB at the time of the Postgres switchover are
migrated nullably.

### 10. Test strategy = `InMemory` for unit tests (Beta), Testcontainers PostgreSQL for repository tests (Block 4)

- **Beta:** All 13 `EfCaptureServiceTests` run against
  `Microsoft.EntityFrameworkCore.InMemory`. Each test gets a fresh DB via a
  unique GUID database name; no file I/O, no provider-specific behaviour. This
  is fast and reliable but **does not exercise SQLite-specific behaviour** — and
  more importantly, would not exercise PostgreSQL-specific behaviour either.
  Acceptable Beta-scope choice; the Beta MVP runs SQLite identically to its
  unit-test environment for the Capture aggregate's current query shapes.
- **Block 4:** Repository tests will move to **Testcontainers PostgreSQL** to
  exercise real provider behaviour (operator semantics, JSONB column shape,
  full-text search, etc.). The 13 existing tests will be parameterized over
  both `InMemory` (fast feedback) and Testcontainers PostgreSQL (provider
  fidelity), or split into two suites with `Category` traits — TBD with
  Block 4's testing-strategy update.
- **API integration tests** (`tests/FlowHub.Api.IntegrationTests/`, 17 tests)
  swap the production Sqlite registration for `InMemory` per the
  `IntegrationTestFactory` pattern documented in §5. The Block 4 evolution will
  swap this to Testcontainers PostgreSQL for parity with production, at the
  cost of slower test boot.

The provider-removal pattern in `IntegrationTestFactory.CreateHost` is
non-trivial (EF Core 8+ registers `IDbContextOptionsConfiguration<TContext>` as
a second descriptor; removing only `DbContextOptions<TContext>` is insufficient
or you get *"two providers registered"* at runtime). The pattern is documented
inline in the test file and called out in `docs/ai-usage.md`'s Beta-MVP
retrospective.

---

## Alternatives considered (and rejected)

### Dapper instead of EF Core

Dapper is the .NET community's idiomatic micro-ORM (raw SQL + manual mapping).
Rejected because:
- The Block 4 Auftrag explicitly asks for "ORM-Abstraktion" — a micro-ORM
  doesn't satisfy that wording.
- FlowHub's query shapes are not Dapper's strength: we want LINQ + Expression
  Trees ("Criteria-API"-Äquivalent per the Auftrag) for dynamic filters; Dapper
  defers all that to hand-written SQL.
- Migration tooling is more mature in EF Core than in Dapper-based projects (no
  first-party Dapper migration story).

### NHibernate instead of EF Core

NHibernate is the older .NET ORM, originally a Hibernate port. Rejected because:
- Smaller community than EF Core in 2026.
- Microsoft's first-party investment is in EF Core (and increasingly
  `Microsoft.Extensions.AI`-style abstractions); going off-piste here adds
  long-term maintenance cost.
- No measurable advantage for FlowHub's query shapes.

### Repository-per-entity pattern (`ICaptureRepository`, `ISkillRepository`, …)

Would add an explicit `IRepository<T>` layer between `ICaptureService` and
`DbContext`. Rejected because:
- `ICaptureService` is already the driving port; adding `ICaptureRepository`
  duplicates abstraction without addition.
- `DbSet<T>` is already a repository in EF Core's design.
- Mocking-for-tests doesn't need it: `EfCaptureServiceTests` use the InMemory
  provider, which is more representative than a hand-rolled mock.
- Pattern adds ceremony per entity; YAGNI per the project guardrails in
  `CLAUDE.md`.

### Auto-apply migrations in `Program.cs` `app.Run()` body (vs. `IHostedService`)

Both apply at startup. Chose `IHostedService` because:
- Cleaner cancellation semantics (`StartAsync(CancellationToken)`).
- Composes cleanly with the `RemoveAll` pattern in `IntegrationTestFactory`
  (the test host removes only the migration runner descriptor, not the entire
  hosted-service set).
- Matches the project's `LoggerMessage` source-gen pattern for EventIds
  5010/5011 — same shape as `LifecycleFaultObserver` and the AI provider boot
  loggers.

---

## Consequences

### EventId namespace

Reserved 5000–5999 for persistence runtime / startup events. Currently used:

- **5010 LogApplyingMigrations** (`MigrationRunner.StartAsync` entry, Information)
- **5011 LogMigrationsApplied** (`MigrationRunner.StartAsync` exit, Information)

Future: 5020-range for connection-pool events (Block 4), 5030-range for
repository-level slow-query traces (Block 5 with OTEL).

### Beta MVP scope acknowledged

Items the Beta intentionally did NOT do, deferred to Block 4 or Block 5:

| Item | Deferred to | Reason |
|------|-------------|--------|
| PostgreSQL provider | Block 4 | Beta scope = SQLite for zero-setup demo |
| `IEntityTypeConfiguration<T>` per entity | Block 4 | Refactor lands when domain expands beyond `Capture` |
| Repository-pattern interfaces in `FlowHub.Core` | (rejected — see above) | Architectural decision, not deferred |
| AI audit fields on `Capture` (`provider`, `model`, `duration_ms`, `was_fallback`) | Block 4 | Tied to ADR 0004 §"Consequences" |
| Migrations as separate init container (12-Factor XII) | Block 5 | Compose / K8s deployment scope |
| Soft-delete strategy | Block 4 | Lifecycle stages already provide pseudo-deletion via `Orphan` / `Unhandled` |
| Full audit fields (`UpdatedAt`, `CreatedBy`) | Block 4 | Single-user system; `CreatedAt` is sufficient until OIDC lands |
| Testcontainers PostgreSQL for repository tests | Block 4 | Beta uses InMemory for speed; provider fidelity in Block 4 |
| `make db-up` / `make db-migrate` Make targets | Block 4 | Tied to PostgreSQL switch |
| `docs/insights/block-4.md` | Block 4 | Captures Block-4 lessons learned |
| ER diagram of full domain | Block 4 | Beta has only `Capture`; full ER is Block 4 deliverable |

### Consequences for next blocks

**Block 4 (Persistence)**:
- Provider switch to PostgreSQL: connection-string change, Testcontainers
  adoption, `IntegrationTestFactory` provider-removal pattern revalidated.
- Domain expansion: 6+ new entities, each with `IEntityTypeConfiguration<T>`,
  fluent-API config, indexes, FKs.
- Dynamic-query helper (`Expression<Func<T, bool>>` builder) for the
  `/captures` filter chips — currently the only dynamic predicate is the
  `CaptureFilter.Stages` `Contains(stage)` expression.
- Add AI audit fields per ADR 0004 §"Consequences for next blocks".
- Revisit the captive-`HttpClient` pattern in `AddHttpClient<T>` +
  `AddSingleton<ISkillIntegration>(sp => sp.GetRequiredService<T>())` flagged
  in the Beta-MVP final code review (orthogonal to persistence but in scope
  for cleanup).

**Block 5 (Deployment)**:
- Move migration application out of the running app; init container in Compose
  / K8s.
- pgvector adoption for embeddings (ADR 0006 — KI-Suche). Postgres-specific.
- Backup / restore tooling discussion (out of scope per Block 5 Nachbereitung's
  geparkt list, but `pg_dump` workflow worth one paragraph).

---

## References

- Brainstorming spec: `docs/superpowers/specs/2026-05-04-beta-mvp-design.md`
- Implementation plan: `docs/superpowers/plans/2026-05-04-beta-mvp.md`
- ADR 0001: `docs/adr/0001-frontend-render-mode-and-architecture.md`
- ADR 0002: `docs/adr/0002-service-architecture-and-async-communication.md`
- ADR 0003: `docs/adr/0003-async-pipeline.md` — EventId namespacing
- ADR 0004: `docs/adr/0004-ai-integration-in-services.md` — promised AI audit fields
- AI Usage living doc: `docs/ai-usage.md` (Block 4 prep — Beta MVP section)
- Block 4 Nachbereitung: `vault/Blöcke/04 Persitence/04 Persitence - c) Nachbereitung.md`
- Bewertungskriterien: `vault/Organisation/Bewertungskriterien.md`
- EF Core 10 docs: https://learn.microsoft.com/en-us/ef/core/
- 12-Factor XII (Admin processes): https://12factor.net/admin-processes

# FlowHub.Persistence

**Layer:** Outbound adapter (driven side) implementing `FlowHub.Core`'s
repository/service ports with EF Core + PostgreSQL. See ADR 0005.

## Contents

- `FlowHubDbContext.cs` / `FlowHubDbContextFactory.cs` — the EF Core context and
  design-time factory (used by the migrations tooling).
- `Entities/` — persistence entities kept **separate** from the domain types
  (`CaptureEntity`, `SkillEntity`, `TagEntity`, `IntegrationEntity`,
  `IntegrationHealthSampleEntity`, `SkillRunEntity`, `ChannelEntity`,
  `AttachmentEntity`) each with its own `IEntityTypeConfiguration`. The
  entity↔domain split keeps EF concerns out of `FlowHub.Core`.
- `EfCaptureService.cs`, `EfSkillRegistry.cs`, `EfIntegrationHealthService.cs` —
  port implementations.
- `CaptureQueryBuilder.cs` — cursor-paginated, index-aligned query construction
  (backs the NfA-01 latency target).
- `FilesystemAttachmentStorage.cs` — `IAttachmentStorage` implementation.
- `Migrations/` — EF Core code-first migrations, committed to Git (NfA-03).

## Conventions

- Migrations run as a separate init step in production — never auto-migrate
  inside `app.Run()` (12-Factor XII). A dev-only `MigrationRunner` auto-applies
  on startup for convenience.
- pgvector / HNSW backs semantic search (ADR 0006).

## Tests

`tests/FlowHub.Persistence.Tests` — run against real PostgreSQL via Testcontainers.

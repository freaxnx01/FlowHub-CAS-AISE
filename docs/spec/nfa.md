# Non-Functional Attributes (NfA) — SMART Criteria

## NfA-01: Query Latency

**Specific:** All Capture list queries (`ICaptureService.ListAsync`) with a limit ≤ 50 must complete within 100ms at p95 under normal load.  
**Measurable:** Measured via OpenTelemetry span duration on the `ListAsync` span; threshold surfaced in Grafana.  
**Achievable:** Index-backed queries on `Stage`, `CreatedAt`, and `MatchedSkill` columns; cursor pagination avoids full-table scans.  
**Relevant:** Dashboard and Captures list are the two highest-traffic read paths.  
**Time-bound:** Verified against Testcontainers PostgreSQL with 10k seeded rows in Slice 4.

## NfA-02: Index Strategy

All high-frequency filter columns carry dedicated B-tree indexes:

| Index | Column(s) | Query pattern |
|---|---|---|
| `IX_Captures_Stage` | `Stage` | Dashboard "Needs Attention", lifecycle filter |
| `IX_Captures_CreatedAt_DESC` | `CreatedAt DESC` | Recent Captures, cursor pagination |
| `IX_Captures_MatchedSkill` | `MatchedSkill` | Skill-based queries |
| `IX_IntegrationHealthSamples_IntegrationName_SampledAt_DESC` | `(IntegrationName, SampledAt DESC)` | Health history queries |

## NfA-03: Migration Strategy

- All schema changes via EF Core migrations (code-first, migration files committed to Git).
- Production apply: `dotnet ef migrations script --idempotent` generates an idempotent SQL script reviewed before each deploy.
- Never `EnsureCreated` or auto-migrate inside `app.Run()` in production — migrations run as a separate init step (12-Factor XII).
- Dev: `MigrationRunner` hosted service auto-applies on startup for developer convenience.

## NfA-04: Data Volume Assumptions

- Captures: up to 100,000 rows in Block 4 scope. Beyond 1M, consider partitioning (out of scope).
- IntegrationHealthSamples: up to 10,000 rows per integration. Prune policy (retain 90 days) deferred to Block 5.
- SkillRuns: up to 500,000 rows. Archival deferred to Block 5.

## NfA-05: Connection Resilience

Npgsql connection pool defaults (min=0, max=100) are sufficient for single-instance dev deployment. Production pool sizing configured via connection string parameters. Connection retries handled by Npgsql's built-in retry policy.

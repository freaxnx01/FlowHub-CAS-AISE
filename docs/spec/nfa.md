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

## NfA-D1: Container Build Time

**Category:** Deployment  
**Statement:** The multi-stage Docker image for `flowhub.web` MUST build from scratch in under 5 minutes on a GitHub-hosted `ubuntu-latest` runner (2 vCPUs, 7 GB RAM).  
**Measurement:** GitHub Actions `release.yml` workflow — `docker/build-push-action` step duration.  
**Target:** ≤ 300 seconds.

## NfA-D2: Image Size

**Category:** Deployment  
**Statement:** The published `flowhub-web` Docker image MUST be under 200 MB compressed.  
**Measurement:** `docker image inspect ghcr.io/freaxnx01/flowhub-web:<version> --format='{{.Size}}'` after pull (uncompressed); GHCR layer sizes (compressed).  
**Target:** ≤ 200 MB compressed (≤ 400 MB uncompressed).

## NfA-D3: Startup Time

**Category:** Deployment  
**Statement:** After migrations complete, `flowhub.web` MUST reach a healthy `/health/live` state within 30 seconds on a cold container start.  
**Measurement:** Docker Compose healthcheck — `interval: 10s, retries: 3` = max 30 s.  
**Target:** `/health/live` returns HTTP 200 within 30 seconds of container start.

## NfA-O1: Observability — Metrics Endpoint

**Category:** Observability  
**Statement:** The running `flowhub.web` process MUST expose Prometheus-format metrics at `/metrics`.  
**Measurement:** `curl http://localhost:5070/metrics` returns HTTP 200 with `Content-Type: text/plain; version=0.0.4`.  
**Target:** Endpoint available and returns at least `dotnet_*` and `http_*` metrics.

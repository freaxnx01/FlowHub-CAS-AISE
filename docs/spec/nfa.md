# Non-Functional Attributes (NfA) — SMART Criteria

Every NfA below follows the SMART template: **Specific** (what), **Measurable** (how it's verified), **Achievable** (technical basis), **Relevant** (why it matters), **Time-bound** (when / under what load).

## Performance

### NfA-P1: Query Latency

**Specific:** All Capture list queries (`ICaptureService.ListAsync`) with a limit ≤ 50 must complete within 100 ms at p95 under normal load.
**Measurable:** OpenTelemetry span duration on the `ListAsync` span; threshold surfaced in Grafana.
**Achievable:** Index-backed queries on `Stage`, `CreatedAt`, `MatchedSkill`; cursor pagination avoids full scans.
**Relevant:** Dashboard and Captures list are the two highest-traffic read paths.
**Time-bound:** Verified against Testcontainers PostgreSQL with 10k seeded rows in Block 4 Slice 4; re-verified per release.

### NfA-P2: Index Coverage

**Specific:** Every high-frequency filter column must have a dedicated B-tree index (see table below).
**Measurable:** Verified via `EXPLAIN ANALYZE` on the four target queries; no `Seq Scan` on tables > 1k rows.
**Achievable:** Indexes created via EF migrations.
**Relevant:** Without indexes, p95 latency degrades super-linearly past 10k rows.
**Time-bound:** Enforced from Block 4 onward; checked in `FlowHub.Persistence.Tests`.

| Index | Column(s) | Query pattern |
|---|---|---|
| `IX_Captures_Stage` | `Stage` | Dashboard "Needs Attention", lifecycle filter |
| `IX_Captures_CreatedAt_DESC` | `CreatedAt DESC` | Recent Captures, cursor pagination |
| `IX_Captures_MatchedSkill` | `MatchedSkill` | Skill-based queries |
| `IX_IntegrationHealthSamples_IntegrationName_SampledAt_DESC` | `(IntegrationName, SampledAt DESC)` | Health history queries |

## Scalability

### NfA-S1: Data Volume Envelope

**Specific:** The system must handle Captures up to 100k rows, IntegrationHealthSamples up to 10k per integration, SkillRuns up to 500k rows — without latency regression beyond NfA-P1.
**Measurable:** Load test seeds the above volumes and re-runs NfA-P1 latency benchmark.
**Achievable:** Index strategy + cursor pagination; partitioning explicitly deferred above 1 M rows.
**Relevant:** Single-user homelab usage stays within these bounds for years.
**Time-bound:** Volume baseline locked at Tag `v0.1.0`; partitioning revisited if a single table exceeds 1 M rows.

### NfA-S2: Connection Resilience

**Specific:** The Npgsql connection pool must absorb at least 100 concurrent active connections without exhaustion; transient PostgreSQL outages of up to 5 s must auto-recover without restart.
**Measurable:** `Npgsql.Connections.PoolExhausted` metric stays at 0 under load; retry policy logs reconnection.
**Achievable:** Npgsql pool `max=100` plus built-in retry policy; circuit-breaker on the integration adapters.
**Relevant:** Homelab Postgres restarts (backup window, upgrades) must not page the user.
**Time-bound:** Verified during Block 5 chaos test (Postgres killed for 5 s, Web container stays healthy).

## Security

### NfA-SE1: Authentication

**Specific:** All HTTP endpoints except `/health/*`, `/metrics`, `/scalar`, and the OIDC callback must require an authenticated principal.
**Measurable:** Integration test `AuthenticationGate_Tests` asserts 401 for anonymous requests on every protected route.
**Achievable:** ASP.NET Core authentication middleware + `[Authorize]` at the controller/page level; OIDC provider = Authentik (homelab).
**Relevant:** FlowHub holds personal Captures and integration credentials — anonymous access would leak both.
**Time-bound:** Production state from Block 5 onward; dev uses `DemoAuthHandler` with auto sign-in.

### NfA-SE2: Secret Handling

**Specific:** No secret (API key, OIDC client secret, DB password) is committed to Git; all secrets resolve from environment variables at runtime.
**Measurable:** `gitleaks` scan green in CI; `appsettings.json` files contain no secret values (only placeholders).
**Achievable:** 12-Factor III config via env; Docker Compose `--env-file`; Authentik client secret rotated via env.
**Relevant:** Public repository on GitHub — any secret leak is immediate.
**Time-bound:** Enforced per commit via CI scan.

## Availability

### NfA-A1: Liveness

**Specific:** A running container must answer `GET /health/live` with HTTP 200 within 30 s of container start.
**Measurable:** Docker Compose healthcheck (`interval: 10s, retries: 3`) — orchestrator restarts unhealthy containers.
**Achievable:** ASP.NET Core health-check middleware; minimal startup work in `Program.cs`.
**Relevant:** Compose/K8s rely on a working liveness probe to detect zombies.
**Time-bound:** From cold container start, every restart.

### NfA-A2: Graceful Shutdown

**Specific:** On `SIGTERM` the application must complete in-flight requests within 15 s before terminating.
**Measurable:** `dotnet` host's `ShutdownTimeout` configured; logs show "graceful shutdown complete" before exit.
**Achievable:** Built-in Kestrel/IHostedService shutdown semantics; MassTransit stops consumers in shutdown hook.
**Relevant:** Rolling deploys must not drop user requests.
**Time-bound:** Verified by the Block 5 "rolling restart" runbook.

## Operations (logs, monitoring, deployment)

### NfA-O1: Metrics Endpoint

**Specific:** `flowhub.web` MUST expose Prometheus-format metrics at `/metrics`.
**Measurable:** `curl http://localhost:5070/metrics` returns HTTP 200 with `Content-Type: text/plain; version=0.0.4`; response contains at least `dotnet_*` and `http_*` series.
**Achievable:** `OpenTelemetry.Exporter.Prometheus.AspNetCore`; instrumented in `Program.cs`.
**Relevant:** Prometheus scrape is the single source of metrics for the Grafana dashboard.
**Time-bound:** Always exposed in production builds.

### NfA-O2: Structured Logging

**Specific:** All application logs are emitted as structured JSON to stdout, with W3C trace-context fields (`TraceId`, `SpanId`).
**Measurable:** `docker logs flowhub.web` produces lines parseable as JSON; every line carries `TraceId`.
**Achievable:** Serilog `WriteTo.Console(new CompactJsonFormatter())`; OpenTelemetry trace enricher.
**Relevant:** Cross-span correlation in Grafana / Loki without per-request manual log decoration.
**Time-bound:** 12-Factor XI compliance from Block 5 onward.

### NfA-O3: Migration Strategy

**Specific:** Schema changes are applied via EF Core migrations only; production never auto-migrates inside `app.Run()`.
**Measurable:** Migration runs as a dedicated init container (`flowhub.migrations` in `docker-compose.yml`) before the web container becomes ready; init container exits 0.
**Achievable:** `dotnet ef migrations script --idempotent` produces a reviewable SQL artifact; init container runs the bundle.
**Relevant:** 12-Factor XII; prevents schema drift and partial migrations under load.
**Time-bound:** Enforced from Block 5 release onward.

## Deployment

### NfA-D1: Container Build Time

**Specific:** The multi-stage Docker image for `flowhub.web` must build from scratch in under 5 minutes on a GitHub-hosted `ubuntu-latest` runner (2 vCPUs, 7 GB RAM).
**Measurable:** GitHub Actions `release.yml` — `docker/build-push-action` step duration.
**Achievable:** Multi-stage build with layer caching on NuGet restore.
**Relevant:** Slow builds throttle release cadence and CI cost.
**Time-bound:** ≤ 300 s, every release build.

### NfA-D2: Image Size

**Specific:** The published `flowhub-web` Docker image must be under 200 MB compressed.
**Measurable:** `docker image inspect ghcr.io/freaxnx01/flowhub-web:<version>` for uncompressed; GHCR layer sizes for compressed.
**Achievable:** `-alpine` base image; trim where practicable.
**Relevant:** Smaller images deploy and pull faster across homelab/CI.
**Time-bound:** ≤ 200 MB compressed (≤ 400 MB uncompressed) for every release tag.

### NfA-D3: Startup Time

**Specific:** After migrations complete, `flowhub.web` must reach a healthy `/health/live` state within 30 s of cold container start.
**Measurable:** Docker Compose healthcheck (`interval: 10s, retries: 3`) flips to `healthy` within ≤ 30 s.
**Achievable:** Lean startup work in `Program.cs`; expensive initialisation deferred behind hosted services.
**Relevant:** Slow starts compound on rolling deploys.
**Time-bound:** Every cold start, in production and dev.


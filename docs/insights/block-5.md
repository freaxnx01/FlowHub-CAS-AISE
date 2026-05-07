# Block 5 — Insights

**Block:** 5 — Deployment & Abgabe  
**Date range:** 2026-05-07 – 2026-07-06

## What We Built

**Containerisation:** Multi-stage Docker images for `flowhub.web` (SDK → Alpine runtime, non-root user) and `flowhub.migrations` (efbundle binary). `.dockerignore` keeps images lean.

**Docker Compose Stack:** 6-service production topology — `flowhub.web`, `flowhub.migrations`, `postgres`, `rabbitmq`, `prometheus`, `grafana`. Migrations run as a `service_completed_successfully` dependency (12-Factor XII). Override file activates demo mode without code changes.

**GitHub Actions:** `ci.yml` (build+test on every push), `release.yml` (Docker image to GHCR on tags), `migrations.yml` (efbundle artifact on migration changes). Branch protection on `main` gates on CI.

**OIDC Auth:** `DemoAuthHandler` replaces `DevAuthHandler` — auth mode now driven by `Auth:OIDC:Authority` presence, not `IsDevelopment()`. Production uses Authentik OIDC.

**Semantic Search:** pgvector `vector(1024)` column on `Captures` with HNSW index. `AiEmbeddingService` wraps Mistral `mistral-embed` via MEAI. `IEmbeddingService` port in `FlowHub.Core`; graceful no-op when key absent. Search endpoint: `GET /api/v1/captures/search?q=...`. Admin rebuild: `POST /api/v1/admin/embeddings/rebuild`.

## Key Decisions

- pgvector built into Npgsql 10 via `Pgvector.EntityFrameworkCore` — no extra vector DB service
- Env-var auth switching (not environment name) — same binary works anywhere
- efbundle migrations binary — no SDK at runtime (12-Factor XII)
- DemoAuthHandler name clarifies intent (works in any environment, not just dev)

## Lessons Learned

- `UseVector()` in Npgsql 10.0.1 requires `Pgvector.EntityFrameworkCore` — the built-in support requires a newer minor release than what was available; always verify package changelog before assuming a feature is in-the-box
- EF Core's `FromSqlRaw` with a float-array vector literal is injection-safe since all values are IEEE 754 floats
- Authentik's OIDC discovery endpoint makes `.AddOpenIdConnect()` configuration minimal — just `Authority`, `ClientId`, `ClientSecret`
- Building efbundle in CI and shipping it as an artifact decouples migration tooling from the runtime image
- 12-Factor XII (migrations as separate step) forces explicit migration management; `make migrate` fills the local dev gap

## AI Tooling

Claude Code was used throughout Block 5 for:
- Writing the implementation plan from the design spec
- Generating GitHub Actions workflow YAML (corrected permissions and artifact paths)
- Scaffolding the `AiEmbeddingService` (corrected `GenerateAsync` call signature for MEAI)
- Writing the `SearchEndpoints` and `AdminEndpoints` (endpoint registration pattern taken from existing `CaptureEndpoints`)

All generated code was reviewed, tested, and adjusted before commit. AI-suggested `Pgvector.EntityFrameworkCore` (a separate package) was the correct approach — Npgsql 10.0.1 does not include built-in vector support without it. The lesson: AI training data may not reflect very recent library releases; verify against installed package versions.

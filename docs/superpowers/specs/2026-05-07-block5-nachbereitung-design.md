# Block 5 Nachbereitung — Design Spec

**Date:** 2026-05-07  
**Phase budget:** 22 h  
**PVA:** 2026-06-20  
**Submission deadline:** 2026-07-04 24:00  
**Repo:** github.com/freaxnx01/FlowHub-CAS-AISE

---

## Goal

Final block. All 18 rubric items (Bewertungskriterien) must be submission-ready. Block 5 adds containerisation, CI/CD, OIDC authentication, semantic search via embeddings, observability, and the submission PDF.

---

## Decisions Made

| Topic | Decision | Rationale |
|---|---|---|
| Container orchestration | Docker Compose only — no Kubernetes | Rubric item fully satisfied by Compose; K8s documented as out-of-scope with rationale |
| Vector store | pgvector on existing PostgreSQL | No new service; Npgsql + EF Core 10 already wired; one NuGet package addition |
| Embedding provider | Mistral `mistral-embed` via MEAI | Existing Mistral credits; OpenAI-compatible API; no new subscription |
| Embedding dimensions | 1024 (`vector(1024)`) | Fixed by `mistral-embed` output; switching provider requires new migration + rebuild |
| Provider abstraction | Env-var configured base URL + model | Switching to OpenAI = change 3 env vars, no code change |
| Auth mode | Env-var driven (`Auth__OIDC__Authority` presence) | Authentik external (homelab); absent = DemoAuthHandler; no `IsDevelopment()` branch |
| Execution order | Rubric-optimised (docs first) | Locks high-point items early; infra gaps can be documented if time runs short |

---

## Architecture

`FlowHub.Api` is a class library — not a standalone process. It registers Minimal API endpoints into `FlowHub.Web`'s route builder. There is **one deployable process**: `FlowHub.Web`, which hosts both the Blazor UI and all API endpoints.

---

## Slice Plan (22 h total)

### Slice 1 — Docs Consolidation (4 h)

Write/finalise all rubric-driven documentation that doesn't depend on Block 5 implementation:

| File | Action |
|---|---|
| `docs/insights/block-1.md` | Write from git log + vault Block 1 notes |
| `docs/insights/block-2.md` | Write from git log + vault Block 2 notes |
| `docs/insights/block-3.md` | Write from git log + vault Block 3 notes |
| `docs/spec/use-cases.md` | Finalise — add Deployment + Search use cases |
| `docs/spec/nfa.md` | Add Deployment NfAs (build time, image size, availability) + Observability NfAs |
| `docs/adr/README.md` | ADR index (ADR 0001–0006) |

### Slice 2 — Containerisation + Compose Stack (5 h)

**Dockerfile** (`source/FlowHub.Web/Dockerfile`) — multi-stage, Alpine, non-root:
1. `build` stage: `mcr.microsoft.com/dotnet/sdk:10.0-alpine` — restore + publish (`--self-contained false`)
2. `runtime` stage: `mcr.microsoft.com/dotnet/aspnet:10.0-alpine` — copy publish output, create `appuser`, `USER appuser`

`.dockerignore` at repo root: excludes `bin/`, `obj/`, `.git/`, `*.user`, test output.

**docker-compose.yml** (production-oriented) — 6 services:

| Service | Image | Notes |
|---|---|---|
| `flowhub.web` | built from `source/FlowHub.Web/Dockerfile` | depends_on: migrations (completed), postgres (healthy), rabbitmq (healthy); healthcheck → `/health/live` |
| `flowhub.migrations` | `alpine:3.20` + efbundle binary | Self-contained migrations bundle; runs once; `restart: on-failure`; 12-Factor XII |
| `postgres` | `postgres:17-alpine` | Init SQL enables pgvector extension; volume `postgres_data` |
| `rabbitmq` | `rabbitmq:3-management-alpine` | Volume `rabbitmq_data`; healthcheck via `rabbitmq-diagnostics ping` |
| `prometheus` | `prom/prometheus:latest` | Scrapes `flowhub.web:5070/metrics` |
| `grafana` | `grafana/grafana:latest` | Volume `grafana_data`; dashboard JSON provisioned from `docs/monitoring/grafana/` |

**docker-compose.override.yml** (dev/demo):
- Omits `Auth__OIDC__*` env vars → DemoAuthHandler activates automatically
- Exposes ports on localhost
- Debug logging level

**Migrations bundle** (`flowhub.migrations` service detail):
- Built at CI time: `dotnet ef migrations bundle --project source/FlowHub.Persistence --startup-project source/FlowHub.Web --output ./efbundle --self-contained`
- Produces a single native executable — no SDK, no source code needed at runtime
- `flowhub.web` depends on `flowhub.migrations` with `condition: service_completed_successfully`

**Postgres init SQL** (`docker/postgres/init.sql`):
```sql
CREATE EXTENSION IF NOT EXISTS vector;
```
Runs once on first container start via Docker's `/docker-entrypoint-initdb.d/` mechanism.

### Slice 3 — GitHub Actions CI/CD (3 h)

Three workflows in `.github/workflows/`:

**`ci.yml`** — triggers on push (all branches) + PRs to `main`:
1. restore → build (warnings-as-errors) → test (all projects) → coverage upload
2. Blocks PR merge on failure; publishes test results as GitHub Check annotations

**`release.yml`** — triggers on tags matching `v*`:
1. Builds Docker image for `flowhub.web`
2. Pushes to GHCR (`ghcr.io/freaxnx01/flowhub-web:vX.Y.Z` + `:latest`)
3. Generates release notes via `orhun/git-cliff-action`
4. Creates GitHub Release with CHANGELOG section

**`migrations.yml`** — triggers on push to `main` (after CI passes):
1. Builds EF Core migrations bundle (`dotnet ef migrations bundle`)
2. Uploads bundle as workflow artifact for manual apply or release pipeline inclusion

**`docs/ci-cd.md`** — documents all three workflows, how to trigger a release, how to apply migrations.

Branch protection on `main`: require CI green before merge.

### Slice 4 — OIDC Authentication (3 h)

**Auth mode switching** — `Program.cs` replaces `IsDevelopment()` branch with config-presence check:

```csharp
if (builder.Configuration["Auth:OIDC:Authority"] is { Length: > 0 } authority)
{
    builder.Services
        .AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
        .AddOpenIdConnect(o => { o.Authority = authority; /* ClientId, ClientSecret from config */ })
        .AddCookie();
}
else
{
    builder.Services
        .AddAuthentication(DemoAuthHandler.SchemeName)
        .AddScheme<AuthenticationSchemeOptions, DemoAuthHandler>(DemoAuthHandler.SchemeName, _ => { });
}
```

`DevAuthHandler` renamed to `DemoAuthHandler` — same auto-sign-in logic, name clarifies it works in any environment without OIDC, not just `Development`.

**Authentik** runs externally in homelab — no Compose service needed.

Production env vars (not committed, injected via `.env` or secrets manager):
```
Auth__OIDC__Authority=https://authentik.home.freaxnx01.ch/application/o/flowhub/
Auth__OIDC__ClientId=flowhub
Auth__OIDC__ClientSecret=<secret>
```

**`docs/runbooks/authentik-oidc-setup.md`** — documents Authentik client registration: OAuth2/OIDC provider, redirect URI `https://<flowhub-host>/signin-oidc`, post-logout URI `/signout-callback-oidc`.

Demo mode: `docker-compose.override.yml` omits `Auth__OIDC__*` → DemoAuthHandler activates. No flag, no environment name, no code branch difference.

### Slice 5 — KI-Suche / Semantic Search (5 h)

**Why:** FlowHub accumulates Captures over time. Keyword search fails when query words don't appear in stored titles/bodies. Embedding-based search finds by meaning — "database performance slow queries" returns a Capture titled "Optimizing EF Core queries for high-throughput APIs". Satisfies the Moodle requirement ("KI-basierende Suche") and the "Intelligente Services mit KI" rubric item (6 pts).

**EF Core migration:**
```sql
CREATE EXTENSION IF NOT EXISTS vector;  -- also in postgres init SQL
ALTER TABLE "Captures" ADD COLUMN "Embedding" vector(1024);
CREATE INDEX captures_embedding_hnsw_idx ON "Captures"
  USING hnsw ("Embedding" vector_cosine_ops);
```

**Embedding provider configuration** (env-var driven, provider-agnostic):
```
Embeddings__BaseUrl=https://api.mistral.ai/v1
Embeddings__ApiKey=<key>
Embeddings__Model=mistral-embed
Embeddings__Dimensions=1024
```

Switching to OpenAI: change `BaseUrl` to `https://api.openai.com/v1`, `Model` to `text-embedding-3-small`, `Dimensions` to `1536` + new migration for column type change + run rebuild endpoint. Documented in ADR 0006.

OpenRouter and Anthropic are **not supported** for embeddings — OpenRouter proxies chat/completion models only; Anthropic has no embeddings API.

**`FlowHub.AI.EmbeddingService`** — wraps `IEmbeddingGenerator<string, Embedding<float>>` (MEAI):
- Input: `"{capture.Title}\n\n{capture.Body}"`
- Called from `ICaptureService.SubmitAsync` after persistence — generates and stores embedding
- Graceful degradation: if `Embeddings__ApiKey` is absent, pipeline is skipped; Capture persists without embedding

**Admin rebuild endpoint:** `POST /api/v1/admin/embeddings/rebuild` — backfills embeddings for all Captures missing them. Required after first deployment or provider switch.

**Search endpoint** (added to `CaptureEndpoints`):
```
GET /api/v1/captures/search?q=<query>&limit=10
```
1. Embed query via `EmbeddingService`
2. Cosine similarity via pgvector (`<=>` operator) in `ICaptureRepository.SearchByEmbeddingAsync`
3. Optional tag/channel filters as additional WHERE clauses
4. Returns same `Capture` shape as `ListAsync`
5. Returns `503 Service Unavailable` if embedding service not configured

**ADR 0006** — documents: Mistral `mistral-embed` as default provider, vector(1024), HNSW index, pgvector over standalone vector store, provider switching procedure.

### Slice 6 — Submission PDF + Final Polish (2 h)

**Remaining docs:**
- `docs/insights/block-5.md` — written after implementation
- `docs/ai-usage.md` — add Block 5 section
- `docs/spec/testing-strategy.md` — add E2E (Playwright) plan + CI test result references

**Submission PDF** content (Markdown → PDF via Pandoc):
1. Vision + Solution overview
2. Use Cases + NfAs (SMART)
3. Architecture — ADR summaries (0001–0006), C4 Context + Container diagrams
4. DB model — ER diagram including `vector(1024)` column
5. Implementation highlights per block (1–5)
6. Test strategy + results (CI run screenshot / coverage report)
7. KI-Nutzung summary (from `docs/ai-usage.md`)
8. KI-Reflexion / Fazit — lessons learned, limits of AI tooling, personal takeaways
9. Repo URL: `github.com/freaxnx01/FlowHub-CAS-AISE`

**Release:**
```bash
git tag v1.0.0 -m "release: v1.0.0 — CAS AISE Projektarbeit Abgabe"
git push origin v1.0.0
```
Upload PDF to Moodle before **2026-07-04 24:00**.

---

## Out of Scope (documented in PDF)

- Kubernetes / Helm chart — Compose fully satisfies the rubric; K8s adds ~6h for no additional points
- Multi-tenancy / RBAC beyond single-user
- Mobile / native clients
- Skill marketplace / plugin loader
- Production backup automation beyond documentation
- OpenRouter / Anthropic as embedding providers (not supported by those platforms)

---

## Rubric Coverage

| Criterion | Max pts | Slice | Deliverable |
|---|---|---|---|
| Use Cases | 5 | 1 | `docs/spec/use-cases.md` final |
| NfA SMART | 5 | 1 | `docs/spec/nfa.md` final |
| Solution Vision | 5 | 6 | PDF section 1 |
| Architektur textuell + bildlich | 7 | 6 | PDF + ADRs 0001–0006 |
| Struktur / Verhalten / Interaktion | 7 | 6 | PDF + existing diagrams |
| DB-Modell | 3 | 5 | Migration + PDF ER diagram |
| Code lesbar/dokumentiert | 7 | 1–5 | All modules, READMEs |
| Erkenntnisse dokumentiert | 3 | 1+6 | `docs/insights/block-1` through `block-5` |
| Source in Git | 2 | 6 | Tag `v1.0.0` on `main` |
| Abnahmekriterien | 5 | 6 | PDF section 6 |
| Test-Strategie | 5 | 1 | `docs/spec/testing-strategy.md` |
| Unit-Tests | 3 | — | Already implemented (Block 4) |
| Test-Ergebnisse | 3 | 3+6 | CI run results in PDF |
| KI-Werkzeug-Nutzung (12 pts ⭐) | 12 | 6 | `docs/ai-usage.md` final |
| Intelligente Services mit KI | 6 | 5 | Embedding pipeline + search endpoint |
| Sub-Systeme als Container | 5 | 2+3 | Compose stack + CI/CD |
| KI-Reflexion / Fazit | 7 | 6 | PDF section 8 |
| ~~Quarkus / Jakarta EE~~ | ~~10~~ | N/A | .NET stack — consciously skipped, noted in PDF |

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

## Test Results (as of 2026-05-12)

`dotnet test FlowHub.slnx --filter "Category!=AI&Category!=BetaSmoke&Category!=E2E"` — **171 tests pass, 0 fail, 0 skip**:

| Project | Passed | Duration | Coverage |
|---|--:|--:|---|
| `FlowHub.Persistence.Tests` | 29 | ~34 s | EF Core + Testcontainers PostgreSQL — `EfCaptureService`, repositories, migrations |
| `FlowHub.Web.ComponentTests` | 92 | ~11 s | bUnit — every Razor page + MassTransit harness pipeline tests |
| `FlowHub.Skills.Tests` | 20 | ~1 s | Unit-level handler tests for Vikunja + Wallabag adapters |
| `FlowHub.Skills.ContractTests` | 13 | ~1 s | **New (Block 5):** WireMock.Net wire-contract tests on a real loopback socket — path, bearer, JSON shape, 401/500 mapping for both skills |
| `FlowHub.Api.IntegrationTests` | 17 | ~17 s | `WebApplicationFactory` + Testcontainers — full HTTP pipeline against real Postgres |

**Excluded by category in default `make test`** (run on demand):

| Trait | Project | When |
|---|---|---|
| `Category=AI` | `FlowHub.AI.IntegrationTests` | `make test-ai` — needs `Ai__*__ApiKey` |
| `Category=BetaSmoke` | `FlowHub.Skills.IntegrationTests` | `make test-beta` — needs live Wallabag + Vikunja |
| `Category=E2E` | `FlowHub.Web.E2ETests` | `make test-e2e` — needs running web server + Playwright Chromium |

CI run reference: latest green `ci.yml` workflow at <https://github.com/freaxnx01/FlowHub-CAS-AISE/actions/workflows/ci.yml>.

## Production-Stack Smoke (`make smoke-prod`, 2026-05-12)

End-to-end probe of the deployment claim from `vault/Blöcke/05 Deployment/05 Deployment - c) Nachbereitung.md`, wired as a make target on 2026-05-12 (commit `f0424ec`). Six steps, last run pass:

```
[1/6] docker compose up --build (detached, --wait until healthy)   ✓
[2/6] verifying flowhub.migrations exited 0                         ✓ (exit 0)
[3/6] GET /health/live                                              ✓ 200
[4/6] GET /metrics — expect dotnet_* and http_* series              ✓ dotnet_* + http_*
[5/6] POST /api/v1/captures (URL capture for embedding round-trip)  ✓ id 738dd52a-…
[6/6] polling Captures.Embedding (up to 30s)                        ✓ populated after ~2s (Mistral)
==> smoke OK — stack left running. Tear down with: make smoke-down
```

The smoke uses a `curlimages/curl:8.10.1` sidecar joined to the `flowhub.web` network namespace, so no host port needs to be published — production network topology is preserved.

## Defects Found by the Smoke Run

The first attempts caught real, latent bugs that would have blocked the submission stack — exactly the kind of finding the smoke target is meant to surface:

1. **Dockerfile missed `.editorconfig`** (commit `4da7ea8`). Both `source/FlowHub.Web/Dockerfile` and `docker/migrations/Dockerfile` copied solution + props + sources but not `.editorconfig`, which silences `CA1707` / `CA1825` on EF Core migration files (auto-generated names starting with `_0001_`). `dotnet publish` inside the build container failed with `TreatWarningsAsErrors`. Fix: add `.editorconfig` to the `COPY` line.

2. **`docker-compose.yml` env-interpolation casing mismatch** (commit `8062bef`). Compose interpolated UPPERCASE `${EMBEDDINGS__APIKEY:-}` while `.env.example` defines mixed-case `Embeddings__ApiKey` (matching the .NET configuration key used everywhere else). Compose substituted an empty string regardless of `.env` contents; the embedding consumer silently no-op'd. Same trap on `Auth__OIDC__*`, plus `Ai__Provider` / `Ai__<Provider>__*` were missing from the compose env block entirely. Fix: switch all interpolations to mixed-case + add the missing `Ai__*` mappings.

3. **Empty-string model strings tripped OpenAI's `AssertNotNullOrEmpty`** (commit `867ada7`). Compose maps `Embeddings__Model: ${Embeddings__Model:-}` which substitutes an empty string when unset. `configuration["Embeddings:Model"] ?? "mistral-embed"` never fired the fallback because the env value was `""` (not null). Same trap on `Ai__<Provider>__Model`. `flowhub.web` crashed with `System.ArgumentException: Value cannot be an empty string. (Parameter 'model')` at startup. Fix: switch to `is { Length: > 0 } m ? m : default` in both spots.

4. **Mistral rejects `dimensions` request field**. The default `.env.example` included `Embeddings__Dimensions=1024`; Mistral's `mistral-embed` returned 422 Unprocessable Entity. (`dimensions` is OpenAI-`text-embedding-3-*` only.) Fix: comment the default in `.env.example` with an explanatory note.

5. **Passbolt `passbolt://` refs were shadowed by Makefile-imported .env**. The Makefile's `-include .env / export` pulled raw `passbolt://<uuid>` strings into recipe env, overriding whatever a parent `passbolt exec` had already resolved. `make ai-classify` therefore *appeared* to succeed (output matched the keyword-classifier fallback exactly — `[task]`, `Wallabag`) but had never actually reached the AI provider. Caught only by inspecting latencies. Fix: a `SECRET_EXEC` wrapper that re-sources `.env` *inside* the recipe shell and reroutes through `passbolt exec --` when present.

Each was committed independently with a one-line "caught by smoke" reference in the commit body; the smoke target paid for itself within a single afternoon.

## AI Tooling

Claude Code was used throughout Block 5 for:
- Writing the implementation plan from the design spec
- Generating GitHub Actions workflow YAML (corrected permissions and artifact paths)
- Scaffolding the `AiEmbeddingService` (corrected `GenerateAsync` call signature for MEAI)
- Writing the `SearchEndpoints` and `AdminEndpoints` (endpoint registration pattern taken from existing `CaptureEndpoints`)

All generated code was reviewed, tested, and adjusted before commit. AI-suggested `Pgvector.EntityFrameworkCore` (a separate package) was the correct approach — Npgsql 10.0.1 does not include built-in vector support without it. The lesson: AI training data may not reflect very recent library releases; verify against installed package versions.

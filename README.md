<p align="center">
  <img src="docs/assets/flowhub-logo.png" alt="FlowHub" width="420" />
</p>

# FlowHub

**AI-assisted personal inbox** — captures everyday information snippets (movie tips, articles, receipts, bookmarks, notes), classifies them with a local/remote LLM, and routes them to the right self-hosted services in the user's homelab. Single user, modular monolith on .NET 10.

Built as the project work for **CAS AI-Assisted Software Engineering (AISE)** at FFHS (FS26).

- **Live demo:** <https://demo.flowhub.freaxnx01.ch> — public, rate-limited, self-resetting; see [`docs/runbooks/public-demo.md`](docs/runbooks/public-demo.md)
- **Submission document:** [`SUBMISSION.md`](SUBMISSION.md) (the PDF rendered from it is the Moodle deliverable)
- **Architecture:** [`docs/adr/`](docs/adr/) (six ADRs) · [`docs/architektur/FlowHub_Arc42_v1_1.pdf`](docs/architektur/FlowHub_Arc42_v1_1.pdf) · [`docs/projektbeschreibung/`](docs/projektbeschreibung/)
- **AI usage:** [`docs/ai-usage.md`](docs/ai-usage.md) · personal Lessons Learned in [`vault/Projektarbeit/Learnings.md`](vault/Projektarbeit/Learnings.md)

---

## Features

- **Capture** — app-bar quick-capture, REST API (`POST /api/v1/captures`), and file upload; any text/URL/file becomes a *Capture*.
- **AI classification** — LLM classifier (Anthropic / OpenRouter via Microsoft.Extensions.AI) with keyword + URL heuristics as a graceful fallback; produces tags + a title.
- **Skill routing** — classified captures route to *Skills* → *Integrations* (Wallabag, Vikunja); outbound payloads carry tag + URL only.
- **Async pipeline** — MassTransit + RabbitMQ consumers (classify, enrich, embed) with per-consumer retry + dead-lettering.
- **Semantic search** — pgvector embeddings + `GET /api/v1/captures/search`.
- **Web UI** — Blazor (MudBlazor, Interactive Server): Dashboard, Captures list/detail, New Capture, Skills, Integrations.
- **Persistence** — PostgreSQL + EF Core 10; migrations run as a separate init container (12-Factor XII).
- **Ops & security** — `/health/*`, `/metrics` (Prometheus) + Grafana, OpenTelemetry, Serilog → stdout; OIDC (Authentik) or dev/demo auth; AI-transparency posture (EU AI Act).
- **Deployment** — multi-stage Docker, full Compose stack, GitHub Actions (CI + tagged release → GHCR).

Full feature list — including **post-`v0.1.0`** product enhancements (citation enrichment, demo example chips, ntfy notifications): **[`FEATURES.md`](FEATURES.md)**.

---

## For a CAS reviewer who just cloned the repo

You probably don't need to run anything to grade this. The submission document and its links cover every rubric item. If you *want* to verify a claim live, the commands below are enough.

### Option A — see it running, zero install

Open <https://demo.flowhub.freaxnx01.ch>. The demo is intentionally open (no login) and resets every 15 min. What it shows — and what's intentionally disabled in the demo (embeddings, external skill writes) — is documented in [`DEMO.md`](DEMO.md) and [`docs/runbooks/public-demo.md`](docs/runbooks/public-demo.md).

### Option B — local stack via Docker

Requires Docker (Compose v2). Boots Web + Postgres + RabbitMQ + Prometheus + Grafana.

```bash
git clone https://github.com/freaxnx01/FlowHub-CAS-AISE.git
cd FlowHub-CAS-AISE
git checkout v0.1.0          # the submission tag
cp .env.example .env         # fill in Postgres password, OIDC, AI keys
docker compose up -d --wait
# → http://localhost:5070  (health: /health/live · metrics: /metrics · API docs: /scalar)
```

Detailed step-by-step including the demo overlay: [`docs/runbooks/v0.1.0-final-acceptance.md`](docs/runbooks/v0.1.0-final-acceptance.md).

### Option C — build and test locally

Requires .NET 10 SDK (`global.json` pins the version). The `justfile` exposes every common task — run `make` with no target for a help listing.

```bash
just build       # build the full solution (warnings as errors)
just test        # run the default suite — 253 tests (excludes live AI / BetaSmoke / E2E)
just run         # run FlowHub.Web on http://localhost:5070
just watch       # same, with hot reload
```

CI build for `v0.1.0`: [GitHub Actions runs](https://github.com/freaxnx01/FlowHub-CAS-AISE/actions/workflows/ci.yml?query=branch%3Amain).

---

## Repository layout

```
source/                 ← application code (one project per capability)
  FlowHub.Core/         ← domain model + driving ports
  FlowHub.Web/          ← Blazor Web App (MudBlazor, Interactive Server)
  FlowHub.Api/          ← Minimal API endpoints (registered into Web)
  FlowHub.AI/           ← LLM-backed classifier + embeddings (MEAI)
  FlowHub.Persistence/  ← EF Core + Npgsql + pgvector
  FlowHub.Skills/       ← Wallabag + Vikunja ISkillIntegration adapters
  FlowHub.Telegram/     ← Telegram capture channel
tests/                  ← 9 test projects (unit, component, integration, E2E)
docs/                   ← architecture, specs, runbooks, insights
  adr/                  ← 6 Architecture Decision Records
  spec/                 ← use-cases, NfA (SMART), acceptance criteria, DB model, testing strategy
  insights/             ← per-block lessons learned (Block 1–5)
  runbooks/             ← acceptance, demo, OIDC setup, test services
  ai-usage.md           ← consolidated AI tool usage (rubric item, 12 pts)
vault/                  ← Obsidian vault — CAS coursework and project notes
  Projektarbeit/        ← idea, dev notes, glossary, learnings
  Blöcke/01..05/        ← per-block Vorbereitung / PVA / Nachbereitung
  Organisation/         ← Bewertungskriterien (Moodle rubric)
SUBMISSION.md           ← the Moodle submission document (rendered to PDF)
submission-notes.md     ← operator notes: how the submission PDF is produced
```

## justfile cheat sheet

`make` with no target prints the help listing. The most useful targets:

### Run + build

| Target | What it does |
|---|---|
| `just run` | Run `FlowHub.Web` on http://localhost:5070 (no hot reload) |
| `just watch` | Same, with `dotnet watch` (hot reload) |
| `just build` | Build the full solution (warnings as errors) |
| `just restore` | Restore NuGet packages |
| `just clean` | Remove build artifacts |
| `just format` | Apply `dotnet format` |

### Tests

| Target | What it does |
|---|---|
| `just test` | All non-live tests (skips AI, BetaSmoke, E2E categories) |
| `just test-backend` | Backend unit + skill contract tests |
| `just test-frontend` | Frontend (bUnit) component tests |
| `just test-all` | Backend + frontend, then start Postgres + Web and open in browser |
| `just test-ai` | Live integration tests against real AI providers (needs API keys) |
| `just test-contract` | WireMock contract tests for Vikunja + Wallabag (offline) |
| `just test-services` | Live skill-integration tests against `flowhub-test-services` |
| `just playwright-install` | One-time Playwright browser install |

### Database

| Target | What it does |
|---|---|
| `just db-up` | Start PostgreSQL in Docker (detached, waits until healthy) |
| `just db-ping` | Verify Postgres connectivity |
| `just db-migrate` | Apply EF Core migrations against the Docker Postgres |

### AI smoke tests

| Target | What it does |
|---|---|
| `just ai-ping` | Smoke-test the configured AI provider with a tiny chat call |
| `just ai-classify "…"` | Run `IClassifier` against an input |
| `just ai-embed "…"` | Run `IEmbeddingService` against an input |

### End-to-end smoke

| Target | What it does |
|---|---|
| `just smoke-prod` | Boot full prod compose stack and smoke health, `/metrics`, capture submit + embedding round-trip |
| `just smoke-down` | Stop the prod stack (volumes preserved) |

### Submission PDFs

| Target | What it does |
|---|---|
| `just pdf-submission` | Render `SUBMISSION.md` → `SUBMISSION.pdf` (the Moodle hub PDF) |
| `just pdf-eigenstaendigkeitserklaerung` | Render `docs/submission/eigenstaendigkeitserklaerung.md` → `Eigenständigkeitserklärung.pdf` (mandatory FFHS beilage) |
| `just pdf-submission-bundle` | Build `SUBMISSION-bundle.pdf` — every referenced artefact inlined (offline safety net) |
| `just pdf-projektbeschreibung` | Regenerate the project description PDF |
| `just pdf … [OUT=…]` | Render any Markdown file to PDF via the puppeteer renderer |

## Configuration

All config is environment-driven (12-Factor III). The main keys:

| Variable | Purpose |
|---|---|
| `ConnectionStrings__Default` | PostgreSQL connection string |
| `Ai__Provider` | `Anthropic` or `OpenRouter` |
| `Ai__Anthropic__ApiKey` / `Ai__OpenRouter__ApiKey` | LLM provider keys |
| `Embeddings__ApiKey`, `Embeddings__Model` | Mistral embeddings (for semantic search) |
| `Skills__Wallabag__*`, `Skills__Vikunja__*` | Real integration endpoints + tokens |
| `Auth__OIDC__*` | Authentik OIDC client (unset → `DemoAuthHandler` auto-signs all requests) |
| `Bus__Transport` | `InMemory` or `RabbitMq` |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | Optional OpenTelemetry collector |

Never commit secrets. See [`docs/runbooks/authentik-oidc-setup.md`](docs/runbooks/authentik-oidc-setup.md) for the OIDC client registration walkthrough.

## Versioning

SemVer 2.0.0. Current submission tag: **`v0.1.0`** (matches `<Version>0.1.0</Version>` in `Directory.Build.props`). See [`CHANGELOG.md`](CHANGELOG.md).

## License

CAS-AISE project work — see repository owner.

## Agent conventions

This repository was developed with heavy AI-assisted engineering. Agent conventions, skills, and the mandatory UI workflow are documented in [`CLAUDE.md`](CLAUDE.md); AI usage per block is in [`docs/ai-usage.md`](docs/ai-usage.md); personal lessons learned in [`vault/Projektarbeit/Learnings.md`](vault/Projektarbeit/Learnings.md).

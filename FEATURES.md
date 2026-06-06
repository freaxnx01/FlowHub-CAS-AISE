# FlowHub — Feature List

> FlowHub is a single-user personal knowledge hub: it **captures** info snippets
> (*Captures*) from any channel, **classifies** them with AI, and **routes** them
> to the right downstream tool.

**Status legend:** ✅ shipped · 🔌 shipped, config-gated (needs real keys/endpoints) ·
🧪 shipped but disabled in the public demo by design · 🗺️ planned / backlog (post-CAS)

---

## Capture & Intake
- ✅ **Quick-capture field** — submit a URL or free text from any page (the Web *Channel*)
- ✅ **REST capture endpoint** — `POST /api/v1/captures` with FluentValidation + RFC 9457 ProblemDetails errors
- ✅ **File upload on capture** — attach a file (2 MB demo cap, `FlowHub:Uploads:MaxBytes`), groundwork for a future paperless-ngx skill
- ✅ **Capture lifecycle state machine** — `Raw → Classified → Routed → Completed`, plus `Orphan` (downstream failed) and `Unhandled` (no matching skill)
- 🗺️ Additional channels: Telegram bot, Signal, Email (architecture-ready via the REST seam; not implemented)

## Classification & AI
- ✅ **Keyword + URL-pattern classifier** — deterministic heuristic (URL → Wallabag, "todo"/"task" → Vikunja, else unsorted); also the safety-net fallback
- 🔌 **LLM classifier** — via Microsoft.Extensions.AI; **Anthropic** (Haiku) and **OpenRouter** (Gemma 4 31B free / Llama) adapters
- ✅ **Graceful AI fallback** — on LLM error/quota/401, automatically falls back to the keyword classifier
- ✅ **AI-transparency marking** — `ClassificationSource` (`None/Heuristic/AI/Manual`) surfaced via a `LifecycleBadge` (EU AI Act Art. 50 hook)

## Skill Routing & Integrations
- ✅ **Skill routing** — a classified Capture is matched to a *Skill* and routed to its *Integration(s)*
- 🔌 **Vikunja integration** — push captures as tasks / list items
- 🔌 **Wallabag integration** — push read-later articles
- ✅ **Outbound data minimization** — skill adapters send only tag + URL, never the Capture body
- 🗺️ Capture **Enrichment** (fetch author/ISBN/preview before routing); paperless-ngx (DMS); Obsidian

## Search & Retrieval
- 🔌 **Semantic / vector search** — `GET /api/v1/captures/search?q=…` over pgvector embeddings (cosine)
- ✅ **Embedding pipeline** — Capture (title + body) → embedding → persisted (async)
- ✅ **Transparent degradation** — 503 ProblemDetails when no embedding key is configured
- 🗺️ Hybrid full-text + vector search (`tsvector` + pgvector)

## Web UI (Blazor + MudBlazor, Interactive Server)
- ✅ **Dashboard** — recent captures, skill/integration health widgets
- ✅ **Captures list** — lifecycle/channel filter chips, text search, pagination
- ✅ **Capture detail** — full view + retry/reassign actions
- ✅ **New Capture** page, **Skills** & **Integrations** read-only grids
- ✅ Mini-drawer layout, app-bar quick-capture, demo-banner support

## API & Interoperability
- ✅ Capture CRUD — `GET/POST /api/v1/captures`, `GET /{id}`, `POST /{id}/retry`
- ✅ **OpenAPI** spec + **Scalar** UI at `/scalar`
- ✅ String-enum JSON contracts; Bruno collections for manual API testing

## Async Processing
- ✅ **MassTransit + RabbitMQ** pipeline — classify, enrich-label, embed consumers
- ✅ Per-consumer **retry policies + dead-lettering**; `Fault<>` observer for lifecycle faults

## Persistence & Data Model
- ✅ **PostgreSQL + EF Core 10** (pgvector image), keyset/cursor pagination, N+1-safe projections
- ✅ Migrations run as a **separate init container** (12-Factor XII); EF bundle in CI
- ✅ Domain: Capture, Skill, SkillRun, Channel, Integration, IntegrationHealthSample, Tag

## Observability & Ops
- ✅ **Health** — `/health/live` (+ readiness groundwork)
- ✅ **Prometheus** `/metrics` + **Grafana** dashboard (checked in)
- ✅ **OpenTelemetry** traces + metrics (incl. MEAI instrumentation); **Serilog → stdout**
- ✅ W3C trace-context correlation across HTTP / EF / MEAI spans

## Security & Data Protection
- 🔌 **OIDC** auth against Authentik (activates when `Auth__OIDC__*` set)
- ✅ **Dev/Demo auth bypass** (`DemoAuthHandler`) when OIDC unset
- ✅ Data-protection posture — household-exemption design, PII-scrubbing logging policy (ADR 0008), telemetry-PII policy (ADR 0009), local-LLM default (ADR 0007)

## Deployment & CI/CD
- ✅ **Multi-stage Dockerfile** (alpine, non-root); full **docker-compose** stack (web + postgres + rabbitmq + prometheus + grafana + migrations)
- ✅ GitHub Actions — **CI** (build/test/coverage), **Release** (tag → GHCR image + git-cliff notes), **Migrations** bundle, Pages
- ✅ SemVer · Conventional Commits · Keep a Changelog

## Public Demo
- 🧪 **Self-resetting public demo** overlay — Traefik-fronted, fully open (`DemoAuthHandler`)
- ✅ **Per-IP rate limiting** (10/min, burst 20) + security headers
- ✅ **15-min data reset** sidecar (truncate + reseed fixtures + purge queues)
- 🧪 Embeddings + skill writes **disabled** in demo (transparent 503 / stops at `Unhandled`); keyword fallback only

---

## Backlog (post-CAS product)
🗺️ Capture Enrichment · web-search tooling (Brave/Tavily) · additional AI providers
(self-hosted Gemma, Apertus, HF router) · full paperless-ngx · Telegram / Signal / Email
channels · multi-user / RBAC

---

*Scope note: this reflects the single-user CAS Projektarbeit scope; 🗺️ items are the
product-spinout roadmap. 🔌/🧪 markers distinguish what runs out-of-the-box from what
needs keys or is intentionally off in the public demo.*

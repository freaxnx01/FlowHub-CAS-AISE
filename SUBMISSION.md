# FlowHub — CAS AISE Submission Document

**CAS AI-Assisted Software Engineering (AISE)** · W4B-C-AS001 · ZH-Sa-1 · FS26
**Student:** Andreas Imboden (`freaxnx01`)
**Repository:** <https://github.com/freaxnx01/FlowHub-CAS-AISE>
**Submission deadline:** before 2026-07-04 24:00 (two weeks after the Block 5 PVA)

---

## About this document

This file is the single entry point for the CAS AISE project submission. It is rendered to PDF and uploaded to Moodle, and deliberately contains **no full content** — every artefact (architecture, ADRs, block notes, reflection, rubric self-check, …) lives in the linked GitHub repository and is reachable through the table of contents below. All links target the `main` branch and are clickable from within the PDF.

---

## 1. Project summary

**FlowHub** is an AI-assisted personal inbox that captures everyday information snippets (movie tips, articles, receipts, bookmarks, notes), classifies them automatically, and forwards them to the right self-hosted services in the user's homelab — without forcing the user to decide where the information belongs at the moment of capture.

The core need addressed is **"capture without friction"**: instead of today's five steps (idea → pick app → open app → categorise → file), FlowHub reduces capture to a single step — typically a message to a Telegram bot. Classification is handled by a **skill-based routing system** (keywords, URL patterns, a local LLM as fallback); storage happens in existing homelab services such as Vikunja, paperless-ngx, Wallabag, or Wekan.

Technically, FlowHub is a **Modular Monolith on .NET 10** with a Blazor frontend (MudBlazor, Interactive Server) and hexagonal layering inside each module. Implementation proceeded incrementally across the five CAS blocks (Foundations, Frontend, Service, Persistence, Deployment) — each block closes with a documented Nachbereitung that is self-assessed against the Moodle grading rubric. An explicit focus is **AI-assisted engineering**: skills, agent instructions, prompt hygiene, and reflection on the AI workflow are part of the documented deliverable and the grading scope.

The full project description with stakeholders, scope, architecture, and risks lives in [`docs/projektbeschreibung/FlowHub_Projektbeschreibung_v4.md`](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/projektbeschreibung/FlowHub_Projektbeschreibung_v4.md).

---

## 2. Repository & demo environment

- **GitHub:** <https://github.com/freaxnx01/FlowHub-CAS-AISE>
- **Branch (submission state):** `main`
- **Live demo:** <https://demo.flowhub.freaxnx01.ch> — see §2.1 below; operating notes in [`docs/runbooks/public-demo.md`](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/runbooks/public-demo.md)
- **License / README:** [`README.md`](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/README.md)
- **Changelog:** [`CHANGELOG.md`](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/CHANGELOG.md)
- **Agent conventions:** [`CLAUDE.md`](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/CLAUDE.md)

### 2.1 What the demo environment shows

The demo at <https://demo.flowhub.freaxnx01.ch> is a self-contained, public instance designed to let any reviewer experience the round-trip "submit a Capture → see it classified by AI → see lifecycle progress" without needing accounts, credentials, or local setup.

**What is wired:**

- **Capture submission** via Quick Capture (any page), Long Form, and the REST API (`POST /api/v1/captures`).
- **AI classification** through `AiClassifier` against OpenRouter `google/gemma-4-31b-it:free`. The `MatchedSkill` and `Classified` lifecycle stage are visible on the dashboard within a few seconds of submission.
- **Keyword-classifier auto-fallback** when OpenRouter is rate-limited or the daily free quota is exhausted — the demo continues serving without a hard error (transparent in the lifecycle metadata).
- **Lifecycle pipeline** end-to-end: `Pending → Classified → Routed → Failed/Unmatched`, fully driven by the MassTransit async pipeline (in-memory transport in the demo overlay).
- **Dashboard & lists** with per-stage counts, recent captures, lifecycle chips, and the captures list with filters and search.
- **Health endpoints:** `/health/live` and `/metrics` are reachable; Prometheus-format metrics include `dotnet_*` and `http_*` series.
- **Open access (no auth):** `DemoAuthHandler` auto-signs every request — fully open by design.
- **Self-reset:** a sidecar truncates Captures + Tags + SkillRuns every 15 minutes and reseeds a small fixture set so the dashboard is never empty.

**What is intentionally disabled (and why):**

- **Real skill integrations** (Wallabag, Vikunja) — captures stop at the `Classified` stage so the demo never writes to anyone's real account. The routing target is still visible as `MatchedSkill` on each capture.
- **Semantic search / embeddings** — the embedding key is unset; `GET /api/v1/captures/search` returns a 503 ProblemDetails with an explanatory body rather than fake results.
- **Prometheus & Grafana UI** — not exposed publicly; metrics are still scraped internally by the operator.

**Operational guard-rails:**

- **Rate limit** at the Traefik edge: 10 req/min average, 20-req burst per source IP.
- **Hard $1/month spend cap** on the OpenRouter key — exhaustion triggers the keyword fallback, never an out-of-pocket bill.
- **Data lifetime ≤ 15 min** — nothing persists across the next reset, including any inappropriate user content.
- **Public-demo posture** documented in detail in [`docs/runbooks/public-demo.md`](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/runbooks/public-demo.md).

---

## 3. Submission artefacts — table of contents

### 3.1 Project description & architecture

- [Project description v4 (Markdown)](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/projektbeschreibung/FlowHub_Projektbeschreibung_v4.md) — current state: vision, stakeholders, scope, architecture
- [Project description v4 (PDF)](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/projektbeschreibung/FlowHub_Projektbeschreibung_v4.pdf)
- [Arc42 architecture documentation v1.1 (PDF)](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/architektur/FlowHub_Arc42_v1_1.pdf) — full Arc42 view
- [Architecture overview v2 (SVG)](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/projektbeschreibung/FlowHub_Architecture-v2.svg)

### 3.2 Architecture Decision Records (ADRs)

- [ADR index](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/adr/README.md)
- [ADR 0001 — Frontend Render Mode & Architecture](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/adr/0001-frontend-render-mode-and-architecture.md)
- [ADR 0002 — Service Architecture & Async Communication](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/adr/0002-service-architecture-and-async-communication.md)
- [ADR 0003 — Async Pipeline](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/adr/0003-async-pipeline.md)
- [ADR 0004 — AI Integration in Services](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/adr/0004-ai-integration-in-services.md)
- [ADR 0005 — Persistence (DB model)](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/adr/0005-persistence.md)
- [ADR 0006 — Vector Search](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/adr/0006-vector-search.md)

### 3.3 Specification & design

- [System Context](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/spec/system-context.md)
- [Use Cases](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/spec/use-cases.md)
- [Non-functional requirements (SMART)](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/spec/nfa.md)
- [Acceptance Criteria](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/spec/acceptance-criteria.md) — Given/When/Then per use case + submission release checklist
- [Testing Strategy](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/spec/testing-strategy.md)
- **Test results (submission snapshot — Tag `v0.1.0`):**

  | Test project | Tests | Layer |
  |---|--:|---|
  | `FlowHub.Web.ComponentTests` | 119 | Blazor components (bUnit) |
  | `FlowHub.Web.E2ETests` | 30 | End-to-end (Playwright) |
  | `FlowHub.Persistence.Tests` | 29 | EF Core + Testcontainers |
  | `FlowHub.Skills.Tests` | 20 | Skill unit tests |
  | `FlowHub.Api.IntegrationTests` | 17 | Minimal-API + ASP.NET TestHost |
  | `FlowHub.Skills.ContractTests` | 13 | Skill contract suite |
  | `FlowHub.AI.IntegrationTests` | 4 | Live LLM (`SkippableFact`, env-gated) |
  | `FlowHub.Skills.IntegrationTests` | 2 | Live skill (`SkippableFact`, env-gated) |
  | **Total** | **234** | |

  Last green CI run for Tag `v0.1.0`: see [GitHub Actions runs](https://github.com/freaxnx01/FlowHub-CAS-AISE/actions/workflows/ci.yml?query=branch%3Amain) (`ci.yml` workflow). Coverage artifacts are uploaded per run; XPlat coverage collector runs across the full solution (`dotnet test FlowHub.slnx --collect:"XPlat Code Coverage"`).

- [**DB model**](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/spec/db-model.md) — ER diagram (Mermaid) + table/index summary; design rationale in [ADR 0005](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/adr/0005-persistence.md); EF migrations under [`source/FlowHub.Persistence/Migrations`](https://github.com/freaxnx01/FlowHub-CAS-AISE/tree/main/source/FlowHub.Persistence/Migrations)
- UI design output (wireframes & flows per feature):
  - [Dashboard](https://github.com/freaxnx01/FlowHub-CAS-AISE/tree/main/docs/design/dashboard)
  - [Captures List](https://github.com/freaxnx01/FlowHub-CAS-AISE/tree/main/docs/design/captures-list)
  - [Capture Detail](https://github.com/freaxnx01/FlowHub-CAS-AISE/tree/main/docs/design/capture-detail)
  - [New Capture](https://github.com/freaxnx01/FlowHub-CAS-AISE/tree/main/docs/design/new-capture)
  - [API](https://github.com/freaxnx01/FlowHub-CAS-AISE/tree/main/docs/design/api)
- POC plans:
  - [AI Classification POC — Design](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/plans/2026-03-09-ai-classification-poc-design.md)
  - [AI Classification POC — Plan](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/plans/2026-03-09-ai-classification-poc-plan.md)

### 3.4 AI usage & reflection

> Highest-weighted grading item (max. 12 pts) — consolidated AI documentation.

- [**AI Usage**](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/ai-usage.md) — tools used (Claude Code, skills, agent instructions), workflow conventions, prompt hygiene, concrete examples per block
- [**Learnings CAS AISE**](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/vault/Projektarbeit/Learnings.md) — personal lessons learned (AI instructions, skill plugins, context hygiene, code exploration)

### 3.5 Block Nachbereitungen (CAS modules)

Each block has three phases: **Vorbereitung** (pre-class preparation), **PVA** (in-class work on the Saturday), and **Nachbereitung** (implementation & reflection). The **Nachbereitungen** are the deliverables graded against the rubric.

**Block 1 — Foundations**

- [Vorbereitung](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/vault/Bl%C3%B6cke/01%20Einf%C3%BChrung/01%20Einf%C3%BChrung%20-%20a)%20Vorbereitung.md)
- [PVA](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/vault/Bl%C3%B6cke/01%20Einf%C3%BChrung/01%20Einf%C3%BChrung%20-%20b)%20PVA.md)
- [**Nachbereitung**](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/vault/Bl%C3%B6cke/01%20Einf%C3%BChrung/01%20Einf%C3%BChrung%20-%20c)%20Nachbereitung.md)

**Block 2 — Frontend**

- [Vorbereitung](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/vault/Bl%C3%B6cke/02%20Frontend/02%20Frontend%20-%20a)%20Vorbereitung.md)
- [PVA](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/vault/Bl%C3%B6cke/02%20Frontend/02%20Frontend%20-%20b)%20PVA.md)
- [**Nachbereitung**](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/vault/Bl%C3%B6cke/02%20Frontend/02%20Frontend%20-%20c)%20Nachbereitung.md)

**Block 3 — Service**

- [Vorbereitung](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/vault/Bl%C3%B6cke/03%20Service/03%20Service%20-%20a)%20Vorbereitung.md)
- [PVA](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/vault/Bl%C3%B6cke/03%20Service/03%20Service%20-%20b)%20PVA.md)
- [**Nachbereitung**](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/vault/Bl%C3%B6cke/03%20Service/03%20Service%20-%20c)%20Nachbereitung.md)

**Block 4 — Persistence**

- [Vorbereitung](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/vault/Bl%C3%B6cke/04%20Persitence/04%20Persitence%20-%20a)%20Vorbereitung.md)
- [PVA](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/vault/Bl%C3%B6cke/04%20Persitence/04%20Persitence%20-%20b)%20PVA.md)
- [**Nachbereitung**](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/vault/Bl%C3%B6cke/04%20Persitence/04%20Persitence%20-%20c)%20Nachbereitung.md)

**Block 5 — Deployment**

- [Vorbereitung](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/vault/Bl%C3%B6cke/05%20Deployment/05%20Deployment%20-%20a)%20Vorbereitung.md)
- [PVA](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/vault/Bl%C3%B6cke/05%20Deployment/05%20Deployment%20-%20b)%20PVA.md)
- [**Nachbereitung**](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/vault/Bl%C3%B6cke/05%20Deployment/05%20Deployment%20-%20c)%20Nachbereitung.md)

### 3.6 Per-block insights

Per-block insights (grading item *Erkenntnisse aus der Programmierung dokumentiert*, max. 3 pts):

- [Block 1 — Foundations](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/insights/block-1.md)
- [Block 2 — Frontend](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/insights/block-2.md)
- [Block 3 — Service](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/insights/block-3.md)
- [Block 4 — Persistence](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/insights/block-4.md)
- [Block 5 — Deployment](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/insights/block-5.md)

### 3.7 Operations & deployment

Operational documentation from Block 5 (grading bucket *Sub-systems / containers*):

- [CI/CD pipeline](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/ci-cd.md)
- [Claude pipeline overview](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/CLAUDE-PIPELINE.md)
- Runbooks:
  - [Authentik OIDC setup](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/runbooks/authentik-oidc-setup.md)
  - [**v0.1.0 final acceptance**](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/runbooks/v0.1.0-final-acceptance.md) — submission-state end-to-end check
  - [Beta-MVP acceptance (Block 4 milestone, historical)](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/runbooks/beta-mvp-acceptance.md)
  - [Public demo](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/runbooks/public-demo.md)
  - [Test services](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/runbooks/test-services.md)
- Monitoring: [Grafana dashboards](https://github.com/freaxnx01/FlowHub-CAS-AISE/tree/main/docs/monitoring/grafana)
- Problem catalogues (RFC 9457):
  - [Capture Not Found](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/problems/capture-not-found.md)
  - [Capture Not Retryable](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/problems/capture-not-retryable.md)
  - [Validation](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/problems/validation.md)
- [Claude-Ready Telegram bot](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/flowhub/claude-ready-bot.md)

### 3.8 Project notes (vault)

Concept and background notes from the Obsidian vault:

- [FlowHub idea](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/vault/Projektarbeit/Idee%20FlowHub.md) — concept notes
- [Dev](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/vault/Projektarbeit/Dev.md) — development notes
- [Skills](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/vault/Projektarbeit/Skills.md) — skill system
- [External services](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/vault/Projektarbeit/External%20Services.md) — integrations
- [Glossary](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/vault/Projektarbeit/Glossary.md)

### 3.9 Grading rubric & self-assessment

- [Bewertungskriterien (Moodle rubric)](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/vault/Organisation/Bewertungskriterien.md) — canonical source: 18 items, 5 buckets, max. 100 pts
- Per-block self-check: lower section of each block Nachbereitung (see 3.5) as a checklist with point weights
- **N/A note:** The Quarkus / Jakarta EE programming criterion (max. 10 pts) is deliberately not applicable to FlowHub — the stack is .NET 10 / ASP.NET Core; rationale recorded in the block Nachbereitungen.

### 3.10 Knowledge base (background)

Deeper notes from the lectures, not part of the primary grading scope but referenced as context:

- [Software Architecture](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/vault/Knowledge/Software%20Architecture.md)
- [UML](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/vault/Knowledge/UML.md)
- [Acronyms](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/vault/Knowledge/Akronyme.md)

---

## 4. Conclusion — what AI delivered and where it stopped

This chapter is the consolidated reflection on AI-assisted engineering across the five CAS blocks. The detailed per-tool catalogue lives in [`docs/ai-usage.md`](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/ai-usage.md); the personal Lessons-Learned essay in [`vault/Projektarbeit/Learnings.md`](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/vault/Projektarbeit/Learnings.md). This section synthesises both.

### 4.1 Where AI delivered most value

- **Block 1 — Foundations.** Claude Code wrote the initial domain model (`Capture`, `LifecycleStage`, `ChannelKind`, port interfaces) from the FlowHub idea note alone. The skeleton was usable in under an hour; what previously took a half-day of typing was reduced to *reviewing* generated code.
- **Block 2 — Frontend.** The mandatory 4-phase UI workflow (`/ui-brainstorm → /ui-flow → /ui-build → /ui-review`) prevented the typical AI failure mode of jumping straight to component code. ASCII wireframes and Mermaid flows became contracts the agent then implemented; bUnit tests were generated together with the components.
- **Block 3 — Service.** Three independent slices (REST API, MassTransit pipeline, AI classifier) were developed via the `superpowers` spec → plan → implement loop with hard `/clear` between phases. This kept context lean and let me parallelise across agents without thread bleed.
- **Block 4 — Persistence.** EF Core migrations, `EfCaptureService`, seed data, and skill/integration adapters (Wallabag, Vikunja) were largely AI-authored. The biggest win: schema decisions stayed traceable in ADR 0005 because the agent was *asked* to draft the ADR before writing code.
- **Block 5 — Deployment.** Multi-stage Dockerfile, GitHub Actions workflows (`ci.yml`, `release.yml`, `migrations.yml`), Grafana dashboards, OIDC integration with Authentik — all scaffolded by Claude Code, reviewed and trimmed by hand. The pgvector + Mistral embedding search (ADR 0006) is the strongest example of "intelligent service built *with* AI."

### 4.2 Where AI hit limits

- **Cross-cutting refactors.** Renames or invariant changes that span 10+ files still need a human to plan the sequence — agents apply local edits well but lose track of subtle ordering (e.g. migrate-then-cleanup, change-port-then-adapter). LSP-driven exploration (see Learnings) only partly mitigated this.
- **Test correctness vs. test coverage.** Agents produce passing tests easily; producing tests that would actually *fail on a regression* required strict TDD discipline (failing test first, never modify a test to make it green). Without the rule baked into `CLAUDE.md`, AI-written tests would have drifted into tautologies.
- **Domain judgement.** "Is this the right abstraction?" remained a human call. The agent would happily generalise three similar lines into a premature abstraction unless explicitly told to prefer duplication until a pattern emerged. The `simplify` and `brainstorming` skills helped, but the final architectural judgement stayed with me.
- **Hallucinated APIs.** MEAI and EF Core 10 are still recent; agents occasionally invented method signatures or used outdated patterns from the pre-`Microsoft.Extensions.AI` era. The `microsoft-code-reference` skill caught most cases, but not all — context7 / official docs lookups remained mandatory checkpoints.
- **Cost / context discipline.** Without the "logs via file" rule (see Learnings §3), debug sessions inflated context to the point of degraded responses within a few iterations. Discipline is needed; the model does not enforce it.

### 4.3 Recommendations for future projects

1. **Treat agent instructions as production code.** `CLAUDE.md`, `.ai/base-instructions.md`, and the skill library are the single biggest leverage point. Invest in them early; refactor them as the project evolves.
2. **Write skills, not prompt snippets.** Skills bring discoverability (trigger descriptions), verifiability (explicit steps), and reusability across agents (Claude Code, Codex, Gemini, Copilot all consume the same files).
3. **Split skills into thematic plugins.** Loading every skill into every session is wasteful; activate only the plugins relevant to today's work. FlowHub-CAS sessions activate `cas-aise-*`, `flowhub`, and a narrow `superpowers` set — nothing else.
4. **`/clear` between phases is not optional.** Spec → plan → implement, each in its own session, each handing off an artefact on disk. This single discipline made multi-day features feasible without context degradation.
5. **Pin grading criteria into the instructions.** The `cas-aise-grade-self-check` skill reads the Moodle rubric and grades against actual repo evidence. Running it at the end of every block kept FlowHub close to "submission-ready" continuously instead of catching gaps in the final week.
6. **Use AI for the things humans dislike doing.** Boilerplate (Dockerfiles, CI YAML, EF migrations, ADR scaffolding, test setup) is where AI saves the most time. Architecture decisions and domain modelling stay collaborative — the agent proposes, the human chooses.

### 4.4 Personal bottom line

FlowHub would have been 2–3× slower without AI assistance, and substantially less consistent. The hard work was not "getting the AI to write code" — that is solved. The hard work was building the surrounding discipline (instructions, skills, context hygiene, phase gates) so that the AI's output was repeatable, reviewable, and aligned with project conventions. That discipline is the deliverable I keep with me beyond this CAS.

---

## 5. Notes for the reviewer

> The mandatory FFHS *Hilfsmittelverzeichnis* and *Eigenständigkeitserklärung* are in §6.

- **Primary entry point** is this file (`SUBMISSION.md`) — every other artefact is reachable via the links in section 3.
- **Codebase** lives in the same repository under [`source/`](https://github.com/freaxnx01/FlowHub-CAS-AISE/tree/main/source) (module structure); tests under [`tests/`](https://github.com/freaxnx01/FlowHub-CAS-AISE/tree/main/tests).
- **Run instructions** and dev conventions: see [`CLAUDE.md`](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/CLAUDE.md) (section *Essential Commands*).
- **AI usage** is documented explicitly — both at the block level (reflection section of each Nachbereitung) and consolidated in the Learnings document (section 3.4). The mandatory FFHS *Hilfsmittelverzeichnis* and *Eigenständigkeitserklärung* are in §6 below, per the FFHS guideline "Umgang mit Gen. KI — Hinweise für Studierende" (01.08.2025).

---

## 6. Hilfsmittelverzeichnis & Eigenständigkeitserklärung

Required for every unsupervised written assessment at FFHS started on or after 1 August 2023, per *"Hinweise und Anforderungen zum Umgang mit generativer künstlicher Intelligenz"* (FFHS, 01.08.2025).

### 6.1 Hilfsmittelverzeichnis

The following table declares every aid used in producing this submission — text, code, documentation, and the submitted PDF itself. A consolidated narrative description of the AI workflow is in [`docs/ai-usage.md`](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/ai-usage.md); the *per-block* AI usage is documented in the reflection sections of each Block Nachbereitung.

| Welches Hilfsmittel wurde eingesetzt? | Wozu wurde das Hilfsmittel eingesetzt? | Betroffene Stellen |
|---|---|---|
| Claude Code (Opus 4.7, 1M context) | Brainstorming, ADR drafting, plan writing, controller for subagent dispatches, code reviews, this Submission Document. | All `docs/superpowers/specs/`, `docs/superpowers/plans/`, `docs/adr/`, `docs/spec/`, `docs/insights/`, `docs/ai-usage.md`, `SUBMISSION.md`, large portions of `source/` and `tests/` (subagent dispatches — see below). |
| Claude Sonnet 4.6 (subagents) | Implementer + spec-reviewer + code-quality-reviewer subagents under the `superpowers` subagent-driven-development workflow. TDD execution, judgement-heavy refactoring. | `source/` and `tests/` changes from Block 3 onward — specifically the async pipeline (Slice B), the REST API (Slice A), the AI classifier (Slice C), and the EF persistence work in Block 4. |
| Claude Haiku 4.5 | Mechanical tasks under the same SDD workflow — project scaffolding, file moves, repetitive registrations, documentation stubs. | Various scaffolding and registration tasks in Blocks 3–5; details per-block in `docs/ai-usage.md`. |
| GitHub Copilot | Inline code suggestions during editing in VS Code. | Sparingly — Claude Code drives sessions end-to-end; Copilot used only for short completions outside dedicated sessions. |
| ChatGPT (GPT-4 family) | Ad-hoc concept clarification and side checks while Claude Code was mid-task on another track. | Occasional cross-checks; not used to generate submitted code or text directly. |
| DeepL / Google Translate | Translation between German and English for selected passages (e.g. Moodle source wording in §2 and the original German vault notes referenced from English documents). | Selected German↔English passages in SUBMISSION.md, `docs/ai-usage.md`, `vault/Projektarbeit/Learnings.md`. |
| Mistral `mistral-embed` (via API) | **Runtime component of the product itself** — embeddings for the FlowHub semantic-search feature (`Captures.Embedding`, ADR 0006). Not used to produce submission text. | Production code only: `FlowHub.AI` embedding adapter; `Captures` table column. |
| OpenRouter (Gemma free tier) | **Runtime component of the public demo only** — classification fallback when the demo's $1 OpenRouter budget is not exhausted. Not used to produce submission text. | Demo environment only (`demo.flowhub.freaxnx01.ch`). |

**Generated-vs-handwritten ratio (estimate):** For implementation code (Blocks 3–5), roughly 70–80 % of the lines were AI-drafted via the subagent workflow and then reviewed, adapted, or rejected by hand. For documentation (ADRs, specs, runbooks, this Submission Document) the share is similar but every artefact passed through manual editing for accuracy and tone. Per-slice breakdowns with concrete numbers are in `docs/ai-usage.md`. All architectural decisions, scope choices, and the final acceptance of every artefact remain the author's.

### 6.2 Eigenständigkeitserklärung

Hiermit erkläre ich,

- dass ich die vorliegende Arbeit selbstständig verfasst habe,
- dass alle sinngemäss und wörtlich übernommenen Textstellen aus fremden Quellen kenntlich gemacht wurden,
- dass alle mit Hilfsmitteln erbrachten Teile der Arbeit präzise deklariert wurden (siehe §6.1 Hilfsmittelverzeichnis),
- dass keine anderen als die im Hilfsmittelverzeichnis aufgeführten Hilfsmittel verwendet wurden,
- dass das Thema, die Arbeit oder Teile davon nicht bereits Gegenstand eines Leistungsnachweises eines anderen Moduls waren, sofern dies nicht ausdrücklich mit der Referentin oder dem Referenten im Voraus vereinbart wurde,
- dass ich mir bewusst bin, dass meine Arbeit elektronisch auf Plagiate und auf Drittautorschaft menschlichen oder technischen Ursprungs überprüft werden kann und ich hiermit der FFHS das Nutzungsrecht so weit einräume, wie es für diese Verwaltungshandlungen notwendig ist.

**Ort, Datum:** Sisseln, vor der Abgabe am 2026-07-04
**Name:** Andreas Imboden
**Unterschrift:** _____________________________

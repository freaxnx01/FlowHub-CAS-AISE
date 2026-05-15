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

## 2. Repository

- **GitHub:** <https://github.com/freaxnx01/FlowHub-CAS-AISE>
- **Branch (submission state):** `main`
- **License / README:** [`README.md`](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/README.md)
- **Changelog:** [`CHANGELOG.md`](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/CHANGELOG.md)
- **Agent conventions:** [`CLAUDE.md`](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/CLAUDE.md)

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
- [Testing Strategy](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/spec/testing-strategy.md)
- DB model: see [ADR 0005 — Persistence](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/adr/0005-persistence.md) plus EF migrations under [`source/FlowHub.Persistence/Migrations`](https://github.com/freaxnx01/FlowHub-CAS-AISE/tree/main/source/FlowHub.Persistence/Migrations)
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
  - [Beta-MVP acceptance](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/runbooks/beta-mvp-acceptance.md)
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

## 4. Notes for the reviewer

- **Primary entry point** is this file (`SUBMISSION.md`) — every other artefact is reachable via the links in section 3.
- **Codebase** lives in the same repository under [`source/`](https://github.com/freaxnx01/FlowHub-CAS-AISE/tree/main/source) (module structure); tests under [`tests/`](https://github.com/freaxnx01/FlowHub-CAS-AISE/tree/main/tests).
- **Run instructions** and dev conventions: see [`CLAUDE.md`](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/CLAUDE.md) (section *Essential Commands*).
- **AI usage** is documented explicitly — both at the block level (reflection section of each Nachbereitung) and consolidated in the Learnings document (section 3.4).

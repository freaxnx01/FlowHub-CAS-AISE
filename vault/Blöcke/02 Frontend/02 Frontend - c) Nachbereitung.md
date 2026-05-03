---
tags:
  - claude-updated
updated: 2026-05-03
---

# Block 2 — Frontend · Nachbereitung

**Phase budget:** 26 h
**PVA war:** 2026-03-21
**Nächste PVA:** 2026-04-25

## Auftrag (Moodle)

- Tech-Entscheid Präsentationsschicht (CSR/SSR/Mix) + Begründung
- Wireframes für FlowHub-Frontend
- Page-Flow-Diagramme
- Frontend implementieren gegen Stub-Services mit Faker-Daten
- KI-generierte Unit-Tests, alle grün
- Master-Detail-Übung in mehreren Varianten

Volltext: `_files/Moodle/Modul/2-Frontend/pdf/W4B-C-AS001.AISE.ZH-Sa-1.PVA.FS26_ Projektarbeit_ Frontend _ Moodle.pdf`

---

## TODO

### ✅ Done

- [x] Master-Detail-Übung (separate Moodle-Aufgabe — siehe `docs/master-detail.html` im Repo)
- [x] **Tech-Entscheid** — ADR 0001 im Repo: `docs/adr/0001-frontend-render-mode-and-architecture.md`
  - Blazor Interactive Server als Default Render Mode
  - OIDC gegen bestehende Authentik (Homelab SSO)
  - Web UI ist selbst ein Channel (`WebChannel`) neben Telegram
  - REST API nur für Non-UI Consumer (Telegram, Integrationen, Automation)
  - Bogus für Faker-Testdaten
- [x] **FlowHub Glossary** — `Projektarbeit/Glossary.md`: Capture, Skill, Channel, Integration, Page/Component/Card/Widget, Render Mode
- [x] **Dashboard wireframe (Phase 1)** — Repo: `docs/design/dashboard/wireframe.md`

### ✅ Scaffolding

- [x] Scaffold `source/FlowHub.Web/` aus dem Blazor Web App Template (Interactive Server, kein WASM)
- [x] MudBlazor verkabeln: `Program.cs`, `App.razor`, `_Imports.razor`, `MainLayout.razor`
- [x] `MudLayout` Shell bauen — `MudAppBar` + Mini `MudDrawer` (click-to-expand) + `MudMainContent` + User Menu
- [x] AppBar **Quick-Capture Field** an Stub `CaptureService.Submit(...)` verdrahtet (WebChannel-Eingang, sichtbar auf jeder Page)
- [x] **`DevAuthHandler`** — fixer `ClaimsPrincipal` "Dev Operator", nur registriert wenn `IsDevelopment()`
- [x] **Bogus** Dependency in `Directory.Packages.props`
- [x] Stub-Service-Interfaces in `FlowHub.Core`: `ICaptureService`, `ISkillRegistry`, `IIntegrationHealthService` + Bogus-basierte Stubs in `source/FlowHub.Web/Stubs/`
- [x] Test-Projekt `tests/FlowHub.Web.ComponentTests/` mit bUnit

### ✅ MVP Path — Per-Page UI Workflow

#### Page 1 — Dashboard (`/`)

- [x] Phase 1 — `/ui-brainstorm` (Wireframe)
- [x] Phase 2 — `/ui-flow` (Mermaid Diagrams)
- [x] Phase 3 — `/ui-build` (Shell + Shared Components + Stubs)
- [x] Phase 4 — `/ui-review` (12 bUnit Tests)

#### Page 2 — New Capture (`/captures/new`)

- [x] Phase 1 — `/ui-brainstorm`
- [x] Phase 2 — `/ui-flow`
- [x] Phase 3 — `/ui-build`
- [x] Phase 4 — `/ui-review` (3 bUnit Tests)

#### Page 3 — Captures list (`/captures`)

- [x] Phase 1 — `/ui-brainstorm`
- [x] Phase 2 — `/ui-flow`
- [x] Phase 3 — `/ui-build` (with lifecycle/channel filter chips + text search + pagination)
- [x] Phase 4 — `/ui-review` (2 bUnit Tests)

### ✅ Stretch — alle 3 geschafft

#### Page 4 — Capture detail (`/captures/{id}`)

- [x] Alle 4 Phasen — inkl. Orphan-Retry / Unhandled-Reassign Action Stubs (snackbar "Coming in Block 3")

#### Page 5 — Skills (`/skills`)

- [x] Implementiert (MudDataGrid + HealthDot, read-only)

#### Page 6 — Integrations (`/integrations`)

- [x] Implementiert (MudDataGrid + HealthDot, read-only)

### ✅ Cross-cutting Verification

- [x] `dotnet test` voll grün — 17 Tests
- [x] `CHANGELOG.md` `[Unreleased]` Section mit Block-2 Deliverables
- [x] Diese TODO-Liste final tick-marken
- [x] ~~Manueller Durchlauf~~ → automatisiert via 14 bUnit Smoke Tests (`SmokeTests.cs`), 31/31 grün

### 🚫 Out of Scope (Block 2) — geparkt

Sind bereits in ADR 0001 als "out of scope" dokumentiert:

- Settings / Preferences Page
- Skill Suggestion Review Queue
- Audit Log Viewer
- Multi-User / RBAC
- Real Authentik Client Registration → Block 5
- Real Persistence → Block 4 (aktuell In-Memory Bogus Stubs)
- Live SignalR Push neuer Captures (nice-to-have, ggf. Block 3)
- Charts / Metrics Visualisierungen

---

## Verweise

- Repo: [[Repository]] — `github.com/freaxnx01/FlowHub-CAS-AISE`
- Glossary: [[Glossary]] — Capture, Skill, Channel, Integration, UI Vocabulary
- ADR 0001: `docs/adr/0001-frontend-render-mode-and-architecture.md` (im Repo)
- Dashboard Wireframe: `docs/design/dashboard/wireframe.md` (im Repo)
- Konzept: [[Idee FlowHub]]

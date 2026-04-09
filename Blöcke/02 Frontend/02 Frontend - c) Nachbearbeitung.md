---
tags:
  - claude-updated
updated: 2026-04-09
---

# Block 2 — Frontend · Nachbearbeitung

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

### 🔨 Scaffolding (one-time, blockt alle per-Page Arbeiten)

- [ ] Scaffold `source/FlowHub.Web/` aus dem Blazor Web App Template (Interactive Server, kein WASM)
- [ ] MudBlazor verkabeln: `Program.cs`, `App.razor`, `_Imports.razor`, `MainLayout.razor`
- [ ] `MudLayout` Shell bauen — `MudAppBar` + Mini `MudDrawer` (click-to-expand) + `MudMainContent` + User Menu
- [ ] AppBar **Quick-Capture Field** an Stub `CaptureService.Submit(...)` verdrahten (WebChannel-Eingang, sichtbar auf jeder Page)
- [ ] **`DevAuthHandler`** — fixer `ClaimsPrincipal` "Dev Operator", nur registriert wenn `IsDevelopment()`. Real Auth Pipeline läuft auch in dev — kein `[AllowAnonymous]`
- [ ] **Bogus** Dependency in `Directory.Packages.props` aufnehmen
- [ ] Stub-Service-Interfaces in `FlowHub.Core` definieren: `ICaptureService`, `ISkillRegistry`, `IIntegrationHealthService` + Bogus-basierte Implementierungen in `source/FlowHub.Web/Stubs/`
- [ ] Test-Projekt `tests/FlowHub.Web.ComponentTests/` mit bUnit + `AuthorizationContext` Helper anlegen

### 📄 MVP Path — Per-Page UI Workflow

Pro Page jeweils 4 Phasen: `/ui-brainstorm` → `/ui-flow` → `/ui-build` → `/ui-review`.
Die Dashboard Phase 3 baut zusätzlich den geteilten Layout-Shell + erste Shared Components (`CaptureRow`, `LifecycleBadge`, `ChannelIcon`, `HealthCard`) — folgende Pages erben davon und werden deutlich kleiner.

#### Page 1 — Dashboard (`/`)

- [x] Phase 1 — `/ui-brainstorm` (Wireframe approved)
- [ ] Phase 2 — `/ui-flow` (Mermaid: Row-Click Navigation, Quick-Capture Submission, Failure Click-Throughs)
- [ ] Phase 3 — `/ui-build` (baut den Shell + erste Shared Components mit)
- [ ] Phase 4 — `/ui-review` (bUnit Tests + Checklist)

#### Page 2 — New Capture (`/captures/new`)

- [ ] Phase 1 — `/ui-brainstorm`
- [ ] Phase 2 — `/ui-flow`
- [ ] Phase 3 — `/ui-build`
- [ ] Phase 4 — `/ui-review`

#### Page 3 — Captures list (`/captures`)

- [ ] Phase 1 — `/ui-brainstorm`
- [ ] Phase 2 — `/ui-flow`
- [ ] Phase 3 — `/ui-build` (heavy Reuse von `CaptureRow`, Lifecycle Filter Chips)
- [ ] Phase 4 — `/ui-review`

### 🌟 Stretch — nur wenn Budget reicht

Realistisch sind 1–2 von diesen 3 Pages, nicht alle.

#### Page 4 — Capture detail (`/captures/{id}`)

- [ ] Alle 4 Phasen — inklusive der Orphan-Retry / Unhandled-Reassign Action Stubs

#### Page 5 — Skills (`/skills`)

- [ ] Alle 4 Phasen

#### Page 6 — Integrations (`/integrations`)

- [ ] Alle 4 Phasen

### 🧪 Cross-cutting Verification (vor nächster PVA)

- [ ] `dotnet test` voll grün
- [ ] Manueller Durchlauf gegen Bogus-Stubs deckt Happy Path + mind. 1 Orphan + 1 Unhandled
- [ ] `CHANGELOG.md` `[Unreleased]` Section mit Block-2 Deliverables aktualisieren
- [ ] Diese TODO-Liste hier final tick-marken

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

## Reading List (offen aus Vorbereitung)

Falls noch nicht erledigt — eigentlich Vorbereitungsarbeit, aber das `02 Frontend - a) Vorbereitung.md` listet diese 8 Kapitel als noch offen:

- [ ] Beyond Vibe Coding · Kap. 3 — The 70% Problem: AI-Assisted Workflows
- [ ] Beyond Vibe Coding · Kap. 4 — Beyond the 70%: Maximizing Human Contribution
- [ ] Coding with AI · Kap. 3 — Design and discovery
- [ ] Coding with AI · Kap. 4 — Coding the first version of our application
- [ ] Coding with AI · Kap. 7 — Building user interfaces with ChatGPT
- [ ] Head First Software Architecture · Kap. 3 — The Two Laws of Software Architecture
- [ ] Head First Software Architecture · Kap. 4 — Logical Components
- [ ] Quarkus in Action · Kap. 6 — Exposing and securing web applications

---

## Verweise

- Repo: [[Repository]] — `github.com/freaxnx01/FlowHub-CAS-AISE`
- Glossary: [[Glossary]] — Capture, Skill, Channel, Integration, UI Vocabulary
- ADR 0001: `docs/adr/0001-frontend-render-mode-and-architecture.md` (im Repo)
- Dashboard Wireframe: `docs/design/dashboard/wireframe.md` (im Repo)
- Konzept: [[Idee FlowHub]]

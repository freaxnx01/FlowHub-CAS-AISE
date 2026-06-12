---
tags:
  - claude-updated
updated: 2026-05-17
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

## Bewertungskriterien (Block 2)

Snapshot am Ende der Block-2-Phase (~2026-04-25) gegen die offizielle Moodle-Rubrik aus [[Bewertungskriterien]]. Punkteangaben in Klammern zeigen Max-Score. Die Rubrik wird kumulativ erst bei Block 5 abschliessend bewertet — diese Sektion dokumentiert den Stand zum Block-2-Abschluss.

> **Sektion nachträglich ergänzt (2026-05-17)** — damit das Pattern aus Block 3 / 4 / 5 auch in Block 1 / 2 vorhanden ist.

> **Rubrik-Update Juni 2026:** Das Programmierkriterium ist jetzt framework-neutral (nicht mehr Quarkus/Jakarta-EE-spezifisch) und für FlowHub (.NET) **direkt erfüllt** — kein ausgeklammertes Item mehr, alle 100 Punkte erreichbar (siehe `vault/Organisation/Bewertungskriterien.md` + `docs/spec/modern-app-concepts.md`).

### Spezifikation

- [-] **Use Cases & fachliche Anforderungen (5)** — informelle UCs über die 6 implementierten Pages (Dashboard, New Capture, Captures, Capture-Detail, Skills, Integrations) + Glossary (`vault/Projektarbeit/Glossary.md`); formales UC-Dokument folgt in Block 3 (`docs/spec/use-cases.md`)
- [-] **NfA SMART (5)** — Frontend-NfAs implizit über ADR 0001 (Render-Mode-Latenz, Stub-Service-Performance); SMART-Formalisierung folgt in Block 3 (`docs/spec/nfa.md`)
- [x] **Solution Vision (5)** — ADR 0001 hält Frontend-Vision (Web UI als Channel, Stub-First, OIDC-Pfad) fest; Idee FlowHub + Glossary konkretisieren die Domäne

### Entwurf

- [x] **Lösungsansatz & Architektur textuell + bildlich (7)** — ADR 0001 (Render Mode + Architektur) + Wireframes (`docs/design/dashboard/wireframe.md` u.a.) + Page-Flow-Diagramme via `/ui-flow`
- [-] **Struktur / Verhalten / Interaktion (7)** — Struktur über MudLayout-Shell + Component-Hierarchie; Verhalten/Interaktion über Mermaid-Page-Flows; voller C4-Container-View folgt in Block 3
- [x] **DB-Modell (3)** — out of scope Block 2 (in-memory Bogus-Stubs); ✅ geliefert Block 4 (ADR 0005, `docs/design/db/er.md`)

### Programmierung

- [x] **Code lesbar / strukturiert (7)** — `source/FlowHub.Web/` sauber gegliedert (Components/Layout, Components/Pages, Components/DashboardCards, Components/Shared, Stubs/); MudBlazor-Konventionen, code-behind via partial class
- [x] ~~Quarkus / Jakarta EE~~ — N/A (Stack: .NET 10)
- [-] **Erkenntnisse dokumentiert (3)** — Block-2-Insights in `docs/insights/block-2.md` (UI-Workflow-Phasen-Disziplin, ADR-Lifecycle); CHANGELOG `[Unreleased]` mit Block-2-Deliverables
- [x] **Source in Git (2)** — `github.com/freaxnx01/FlowHub-CAS-AISE`, Block-2-Commits gepusht

### Validierung

- [-] **Abnahmekriterien (5)** — die 17 + 14 bUnit-Tests dienen als executable acceptance per Page; formalisierte AC-Liste folgt Block 3 (`docs/spec/acceptance-criteria.md`)
- [-] **Test-Strategie + Technologien (5)** — bUnit + xUnit + FluentAssertions + NSubstitute etabliert; dediziertes `docs/spec/testing-strategy.md` folgt Block 3
- [x] **Unit-Tests (3)** — 17 bUnit-Tests + 14 Smoke-Tests (`SmokeTests.cs`), 31/31 grün
- [x] **Test-Ergebnisse dokumentiert (3)** — CHANGELOG `[Unreleased]` mit Test-Counts; `dotnet test` voll grün

### KI, Sub-Systeme & Reflexion

- [-] **KI-Werkzeug-Nutzung beschrieben (12)** ⭐ — `/ui-brainstorm → /ui-flow → /ui-build → /ui-review` Pipeline als KI-gestützter Workflow etabliert und in Block-2-Insights dokumentiert; konsolidiertes `docs/ai-usage.md` folgt Block 3
- [x] **Intelligente Services mit KI (6)** — out of scope Block 2 (Stubs-only); ✅ geliefert Block 3 (`KeywordClassifier` + `AiClassifier`) mit `KeywordClassifier` + `AiClassifier`
- [x] **Sub-Systeme als Container (5)** — out of scope Block 2; ✅ geliefert Block 5 (docker-compose Stack)
- [-] **KI-Reflexion / Fazit (7)** — Block-2-Insights enthalten Workflow-Reflexion (Phase-Disziplin, KI-generierte Tests, MudBlazor-Adaptionen); finales Fazit gehört ins Submission-PDF (Block 5)

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

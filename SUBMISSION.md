# FlowHub – CAS AISE Projektabgabe

**CAS AI-Assisted Software Engineering (AISE)** · W4B-C-AS001 · ZH-Sa-1 · FS26
**Student:** Andreas Imboden (`freaxnx01`)
**Repository:** <https://github.com/freaxnx01/FlowHub-CAS-AISE>
**Abgabedatum:** Mai 2026

---

## Hinweis zum Aufbau dieses Dokuments

Diese Datei ist die zentrale Einreichungs-Seite für die CAS-AISE-Projektarbeit. Sie wird als PDF in Moodle hochgeladen und enthält bewusst **keine** vollständigen Inhalte — alle Artefakte (Architektur, ADRs, Block-Nachbereitungen, Reflexion, Bewertungskriterien-Selfcheck, …) liegen im verlinkten GitHub-Repository und sind über das Inhaltsverzeichnis weiter unten direkt erreichbar. Sämtliche Links zeigen auf den `main`-Branch und sind aus dem PDF heraus klickbar.

---

## 1. Projektzusammenfassung

**FlowHub** ist ein KI-gestützter persönlicher Eingangskorb, der Informationsschnipsel aus dem Alltag (Filmtipps, Artikel, Belege, Bookmarks, Notizen) automatisch erkennt, klassifiziert und an die passenden Self-Hosted-Services im Homelab des Benutzers weiterleitet — ohne dass der Benutzer im Moment der Erfassung entscheiden muss, wohin die Information gehört.

Das adressierte Kernbedürfnis ist **"Capture without friction"**: Statt heute fünf Schritte (Idee → App-Wahl → App öffnen → Kategorisieren → Ablegen) reduziert FlowHub die Erfassung auf einen einzigen Schritt — typischerweise eine Nachricht an einen Telegram-Bot. Die Klassifikation übernimmt ein **Skill-basiertes Routing-System** (Keywords, URL-Muster, lokales LLM als Fallback), die Ablage erfolgt in bestehende Homelab-Services wie Vikunja, paperless-ngx, Wallabag oder Wekan.

Technisch ist FlowHub ein **Modular Monolith in .NET 10** mit Blazor-Frontend (MudBlazor, Interactive Server) und einer hexagonalen Schichtung innerhalb der Module. Die Implementierung erfolgt inkrementell über die fünf CAS-Blöcke (Einführung, Frontend, Service, Persistence, Deployment) — jeder Block schliesst mit einer dokumentierten Nachbereitung ab, die gegen die Moodle-Bewertungskriterien selbst geprüft wird. Ein expliziter Fokus liegt auf **KI-unterstützter Entwicklung**: Skills, Agent-Instructions, Prompt-Hygiene und Reflexion über den AI-Workflow sind dokumentiert und Teil des Bewertungsumfangs.

Die ausführliche Projektbeschreibung mit Stakeholdern, Funktionsumfang, Architektur und Risiken liegt unter [`docs/projektbeschreibung/FlowHub_Projektbeschreibung_v4.md`](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/projektbeschreibung/FlowHub_Projektbeschreibung_v4.md).

---

## 2. Repository

- **GitHub:** <https://github.com/freaxnx01/FlowHub-CAS-AISE>
- **Branch (Abgabestand):** `main`
- **Lizenz / README:** [`README.md`](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/README.md)
- **Changelog:** [`CHANGELOG.md`](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/CHANGELOG.md)
- **Agent-Konventionen:** [`CLAUDE.md`](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/CLAUDE.md)

---

## 3. Inhaltsverzeichnis der Abgabe-Artefakte

### 3.1 Projektbeschreibung & Architektur

- [Projektbeschreibung v4 (Markdown)](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/projektbeschreibung/FlowHub_Projektbeschreibung_v4.md) — aktueller Stand: Vision, Stakeholder, Funktionsumfang, Architektur
- [Projektbeschreibung v4 (PDF)](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/projektbeschreibung/FlowHub_Projektbeschreibung_v4.pdf)
- [Arc42-Architekturdokumentation v1.1 (PDF)](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/architektur/FlowHub_Arc42_v1_1.pdf) — vollständige Arc42-Sicht
- [Architektur-Übersicht v2 (SVG)](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/projektbeschreibung/FlowHub_Architecture-v2.svg)

### 3.2 Architecture Decision Records (ADRs)

- [ADR 0001 — Frontend Render Mode & Architecture](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/adr/0001-frontend-render-mode-and-architecture.md)
- [ADR 0002 — Service Architecture & Async Communication](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/adr/0002-service-architecture-and-async-communication.md)

### 3.3 Spezifikation & Design

- [System Context](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/spec/system-context.md)
- [Use Cases](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/spec/use-cases.md)
- [Testing Strategy](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/spec/testing-strategy.md)
- UI-Design-Output (Wireframes & Flows pro Feature):
  - [Dashboard](https://github.com/freaxnx01/FlowHub-CAS-AISE/tree/main/docs/design/dashboard)
  - [Captures-List](https://github.com/freaxnx01/FlowHub-CAS-AISE/tree/main/docs/design/captures-list)
  - [Capture-Detail](https://github.com/freaxnx01/FlowHub-CAS-AISE/tree/main/docs/design/capture-detail)
  - [New Capture](https://github.com/freaxnx01/FlowHub-CAS-AISE/tree/main/docs/design/new-capture)
  - [API](https://github.com/freaxnx01/FlowHub-CAS-AISE/tree/main/docs/design/api)
- POC-Pläne:
  - [AI-Classification POC — Design](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/plans/2026-03-09-ai-classification-poc-design.md)
  - [AI-Classification POC — Plan](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/plans/2026-03-09-ai-classification-poc-plan.md)

### 3.4 Block-Nachbereitungen (CAS-Module)

Jeder Block hat drei Phasen: **Vorbereitung** (vor dem Präsenztag), **PVA** (Präsenz-/Vor-Ort-Arbeit am Samstag) und **Nachbereitung** (Implementierung & Reflexion). Die **Nachbereitungen** sind die jeweils gegen die Bewertungskriterien geprüften Liefergegenstände.

**Block 1 — Einführung**

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

### 3.5 Projektarbeit & Reflexion

- [Idee FlowHub](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/vault/Projektarbeit/Idee%20FlowHub.md) — Konzept-Notizen
- [Dev](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/vault/Projektarbeit/Dev.md) — Entwicklungsnotizen
- [Skills](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/vault/Projektarbeit/Skills.md) — Skill-System
- [External Services](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/vault/Projektarbeit/External%20Services.md) — Integrationen
- [Glossary](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/vault/Projektarbeit/Glossary.md)
- [**Learnings CAS AISE**](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/vault/Projektarbeit/Learnings.md) — persönliche Lessons Learned (AI-Instructions, Skill-Plugins, Context Hygiene, Code-Exploration)

### 3.6 Bewertungskriterien & Selbsteinschätzung

- [Bewertungskriterien (Moodle-Rubric)](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/vault/Organisation/Bewertungskriterien.md) — kanonische Quelle: 18 Items, 5 Buckets, max. 100 Punkte
- Selbst-Check pro Block: jeweils im unteren Abschnitt der Block-Nachbereitung (siehe 3.4) als Checkliste mit Punktgewichtung
- **N/A-Hinweis:** Das Quarkus-/Jakarta-EE-Programmierkriterium (max. 10 Pkt.) ist für FlowHub bewusst nicht anwendbar — der Stack ist .NET 10 / ASP.NET Core; Begründung in den Block-Nachbereitungen vermerkt.

### 3.7 Knowledge Base (Hintergrund)

Vertiefende Notizen aus den Vorlesungen, nicht Teil der primären Bewertung, aber als Kontext referenziert:

- [Software Architecture](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/vault/Knowledge/Software%20Architecture.md)
- [UML](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/vault/Knowledge/UML.md)
- [Akronyme](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/vault/Knowledge/Akronyme.md)

---

## 4. Hinweise für die Bewertung

- **Primärer Einstiegspunkt** ist diese Datei (`SUBMISSION.md`) — alle anderen Artefakte sind über die Links in Abschnitt 3 erreichbar.
- **Codebasis** liegt im selben Repository unter [`source/`](https://github.com/freaxnx01/FlowHub-CAS-AISE/tree/main/source) (Modulstruktur), Tests unter [`tests/`](https://github.com/freaxnx01/FlowHub-CAS-AISE/tree/main/tests).
- **Run-Anleitung** und Dev-Konventionen siehe [`CLAUDE.md`](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/CLAUDE.md) (Abschnitt *Essential Commands*).
- **KI-Nutzung** ist explizit dokumentiert — sowohl auf Block-Ebene (Reflexionsabschnitt der jeweiligen Nachbereitung) als auch konsolidiert im Learnings-Dokument (Abschnitt 3.5).

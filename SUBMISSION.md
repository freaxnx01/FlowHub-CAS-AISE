# FlowHub – CAS AISE Projektabgabe

**CAS AI-Assisted Software Engineering (AISE)** · W4B-C-AS001 · ZH-Sa-1 · FS26
**Student:** Andreas Imboden
**Repository:** <https://github.com/freaxnx01/FlowHub-CAS-AISE>
**Abgabedatum:** 2026-07-04

---

## Hinweis zum Aufbau dieses Dokuments

Diese Datei ist die zentrale Einreichungs-Seite für die CAS-AISE-Projektarbeit. Sie wird als PDF in Moodle hochgeladen und enthält bewusst **keine** vollständigen Inhalte — alle Artefakte (Architektur, ADRs, Block-Nachbereitungen, Reflexion, Bewertungskriterien-Selfcheck, …) liegen im verlinkten GitHub-Repository und sind über das Inhaltsverzeichnis weiter unten direkt erreichbar. Sämtliche Links zeigen auf den `main`-Branch und sind aus dem PDF heraus klickbar.

---

## 1. Projektzusammenfassung

**FlowHub** ist ein KI-gestützter persönlicher Eingangskorb, der Informationsschnipsel aus dem Alltag (Filmtipps, Artikel, Belege, Bookmarks, Notizen) automatisch erkennt, klassifiziert und an die passenden Ziel-Dienste des Benutzers weiterleitet — self-hosted im Homelab oder extern (z. B. Cloud-Dienste, Forge-Tracker) — ohne dass der Benutzer im Moment der Erfassung entscheiden muss, wohin die Information gehört.

Das adressierte Kernbedürfnis ist **"Capture without friction"**: Statt heute fünf Schritte (Idee → App-Wahl → App öffnen → Kategorisieren → Ablegen) reduziert FlowHub die Erfassung auf einen einzigen Schritt — typischerweise eine Nachricht an einen Telegram-Bot. Die Klassifikation übernimmt ein **Skill-basiertes Routing-System**: ein LLM (Cloud-Provider wie OpenRouter oder ein lokales LLM) klassifiziert den Schnipsel, mit deterministischem Keyword-/URL-Muster-Matching als Fallback. Die Ablage erfolgt in bestehende Dienste — self-hosted im Homelab (z. B. Vikunja, paperless-ngx, Wallabag) ebenso wie externe Services (z. B. GitHub Issues).

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

- [Vorbereitung](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/vault/Bl%C3%B6cke/01%20Einf%C3%BChrung/01%20Einf%C3%BChrung%20-%20a%29%20Vorbereitung.md)
- [PVA](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/vault/Bl%C3%B6cke/01%20Einf%C3%BChrung/01%20Einf%C3%BChrung%20-%20b%29%20PVA.md)
- [**Nachbereitung**](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/vault/Bl%C3%B6cke/01%20Einf%C3%BChrung/01%20Einf%C3%BChrung%20-%20c%29%20Nachbereitung.md)

**Block 2 — Frontend**

- [Vorbereitung](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/vault/Bl%C3%B6cke/02%20Frontend/02%20Frontend%20-%20a%29%20Vorbereitung.md)
- [PVA](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/vault/Bl%C3%B6cke/02%20Frontend/02%20Frontend%20-%20b%29%20PVA.md)
- [**Nachbereitung**](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/vault/Bl%C3%B6cke/02%20Frontend/02%20Frontend%20-%20c%29%20Nachbereitung.md)

**Block 3 — Service**

- [Vorbereitung](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/vault/Bl%C3%B6cke/03%20Service/03%20Service%20-%20a%29%20Vorbereitung.md)
- [PVA](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/vault/Bl%C3%B6cke/03%20Service/03%20Service%20-%20b%29%20PVA.md)
- [**Nachbereitung**](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/vault/Bl%C3%B6cke/03%20Service/03%20Service%20-%20c%29%20Nachbereitung.md)

**Block 4 — Persistence**

- [Vorbereitung](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/vault/Bl%C3%B6cke/04%20Persitence/04%20Persitence%20-%20a%29%20Vorbereitung.md)
- [PVA](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/vault/Bl%C3%B6cke/04%20Persitence/04%20Persitence%20-%20b%29%20PVA.md)
- [**Nachbereitung**](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/vault/Bl%C3%B6cke/04%20Persitence/04%20Persitence%20-%20c%29%20Nachbereitung.md)

**Block 5 — Deployment**

- [Vorbereitung](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/vault/Bl%C3%B6cke/05%20Deployment/05%20Deployment%20-%20a%29%20Vorbereitung.md)
- [PVA](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/vault/Bl%C3%B6cke/05%20Deployment/05%20Deployment%20-%20b%29%20PVA.md)
- [**Nachbereitung**](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/vault/Bl%C3%B6cke/05%20Deployment/05%20Deployment%20-%20c%29%20Nachbereitung.md)

### 3.5 Projektarbeit & Reflexion

- [Idee FlowHub](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/vault/Projektarbeit/Idee%20FlowHub.md) — Konzept-Notizen
- [Dev](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/vault/Projektarbeit/Dev.md) — Entwicklungsnotizen
- [Skills](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/vault/Projektarbeit/Skills.md) — Skill-System
- [External Services](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/vault/Projektarbeit/External%20Services.md) — Integrationen
- [Glossary](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/vault/Projektarbeit/Glossary.md)
- [**Learnings CAS AISE**](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/vault/Projektarbeit/Learnings.md) — persönliche Lessons Learned (AI-Instructions, Skill-Plugins, Context Hygiene, Code-Exploration)

### 3.6 Bewertungskriterien & Selbsteinschätzung

- **Bewertungskriterien (Moodle-Rubric)** — kanonische Quelle: 18 Items, 5 Buckets, max. 100 Punkte.
  - Die offizielle Moodle-Rubrik ist FFHS-Lehrmaterial und wird **nicht im öffentlichen Repository mitveröffentlicht**.
  - Die **Selbsteinschätzung pro Block** (siehe 3.4) bildet die relevanten Kriterien mit Punktgewichtung ab — als Checkliste am Ende jeder Block-Nachbereitung.
- **[Lernziele-Coverage (alle Blöcke)](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/lernziele-coverage.md)** — die *andere* Achse zur Rubrik:
  - jedes Block-Lernziel (Vorbereitung + Nachbereitung) auf konkrete Code- **und** Dokument-Belege abgebildet, inkl. .NET-Stack-Mapping;
  - bewusst zurückgestellter Scope ist dokumentiert: SOAP, Service-Mesh, Kubernetes, Cloud-IaaS, Agentic AI.
- **Programmierkriterium „Konzepte des gewählten Frameworks" (max. 10 Pkt.)** — in der aktuellen Rubrik (Update Juni 2026) **framework-neutral** (nicht mehr Quarkus-/Jakarta-EE-spezifisch). Für .NET 10 / ASP.NET Core **direkt und vollständig erfüllt**; die genannten Konzepte sind im Code nachgewiesen:
  - **Dependency Injection** — `IServiceCollection`, per-Modul-Registrierung
  - **REST-Schnittstellen** — Minimal API + RFC 9457 ProblemDetails (`FlowHub.Api`)
  - **Konfiguration** — `IConfiguration`/Options, 12-Factor
  - **Fehlerbehandlung** — ProblemDetails, MassTransit-Retry + deterministischer Fallback
  - Belege je Konzept: [`docs/spec/modern-app-concepts.md`](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/spec/modern-app-concepts.md). **Kein ausgeklammertes Item mehr — alle 100 Punkte erreichbar** (kein „/90"-Sonderfall).
- **Sub-System-Kriterium (max. 5 Pkt.)** — die Rubrik akzeptiert **explizit den modularen Monolithen** („modularer Monolith oder verteilte Services … als Container lauffähig betrieben"). FlowHub erfüllt das vollständig:
  - klar abgegrenzte Module (ADR 0002);
  - als Container-Stack lauffähig (Docker Compose + Live-Demo).

### 3.7 Knowledge Base (Hintergrund)

Vertiefende Notizen aus den Vorlesungen, nicht Teil der primären Bewertung, aber als Kontext referenziert:

- [Software Architecture](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/vault/Knowledge/Software%20Architecture.md)
- [UML](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/vault/Knowledge/UML.md)
- [Akronyme](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/vault/Knowledge/Akronyme.md)

### 3.8 Ausblick & Erweiterbarkeit

Die [**Roadmap**](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/project/ROADMAP.md) zeigt das Weiterentwicklungspotenzial — und ist zugleich der beste Beleg für die **Tragfähigkeit der Architektur**: Weil FlowHub ein hexagonaler Modular Monolith ist, sind die meisten Erweiterungen **dünne Adapter** an bestehenden Ports, kein Umbau des Kerns:

- **Neue Capture-Kanäle** (Telegram, OS-/PWA-Share-Target, E-Mail) → je ein *driving adapter* vor `ICaptureService`.
- **Neue Skill-Ziele** (paperless-ngx inkl. dessen eigener KI, GitHub-Issues z. B. `flowhub: Add i18n DE/EN`, Karakeep, Immich, Firefly) → je eine `ISkillIntegration`-Implementierung.
- **KI-Tiefe** (agentische Mehrschritt-Klassifikation, lokales LLM via Ollama für volle Datenresidenz, Confidence-basierter Human-in-the-Loop, semantische Features auf dem vorhandenen pgvector-Index) → hinter den bestehenden `IClassifier`/Provider-Abstraktionen.
- **Skalierungspfad** (eigenständiger Worker-Container, Multi-User via OIDC, vollständiges Tracing) → bewusst *reversible* Konsequenz der Modular-Monolith-Entscheidung (ADR 0002): per Konfiguration, nicht per Neuentwicklung.

> Dies ist **Potenzial, nicht zugesagter Abgabe-Umfang** (vgl. Scope-Freeze) — aufgeführt, um die Erweiterbarkeit des Entwurfs zu zeigen.

---

## 4. Hinweise für die Bewertung

- **Primärer Einstiegspunkt** ist diese Datei (`SUBMISSION.md`) — alle anderen Artefakte sind über die Links in Abschnitt 3 erreichbar.
- **Codebasis** liegt im selben Repository unter [`source/`](https://github.com/freaxnx01/FlowHub-CAS-AISE/tree/main/source) (Modulstruktur), Tests unter [`tests/`](https://github.com/freaxnx01/FlowHub-CAS-AISE/tree/main/tests).
- **Test-Ergebnisse:** der letzte CI-Lauf auf `main` ([Run #27510188698, 2026-06-14](https://github.com/freaxnx01/FlowHub-CAS-AISE/actions/runs/27510188698)) ist **grün — 294 Tests, 0 Fehler** über 6 Test-Projekte (Core-Unit, Web-Component/bUnit, Api-Integration, Persistence, Skill-Contract/WireMock; E2E-Playwright und Live-AI-Tests sind kategoriebasiert separat). Coverage wird via *XPlat Code Coverage* erhoben (CI-Artefakt). Test-Strategie: [`docs/spec/testing-strategy.md`](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/spec/testing-strategy.md).
- **Run-Anleitung** und Dev-Konventionen siehe [`CLAUDE.md`](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/CLAUDE.md) (Abschnitt *Essential Commands*).
- **KI-Nutzung** ist explizit dokumentiert — sowohl auf Block-Ebene (Reflexionsabschnitt der jeweiligen Nachbereitung) als auch konsolidiert im Learnings-Dokument (Abschnitt 3.5).
- **Monitoring & Observability** (Block-5-Lernziel „Systeme überwachen und optimieren") ist umgesetzt und betreibbar: OpenTelemetry-Metriken → Prometheus (`/metrics`) → Grafana-Dashboard, Health-Endpoint (`/health/live`), In-App-Integration-Health (Vikunja/Wallabag/Paperless) und eine öffentliche **Uptime-Kuma**-Statusseite auf der Demo (`status.demo.flowhub.freaxnx01.ch`), die auch die LLM-Erreichbarkeit prüft. Self-Healing über Container-`restart`-Policies + Healthchecks; KeywordClassifier als Fallback bei LLM-Ausfall. Details: [`docs/ci-cd.md`](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/ci-cd.md) und [`docs/runbooks/public-demo.md`](https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/runbooks/public-demo.md).

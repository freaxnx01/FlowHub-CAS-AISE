---
tags:
  - claude-generated
  - claude-updated
updated: 2026-05-02
---

# Block 3 — Service · Nachbereitung

**Phase budget:** 26 h
**PVA war:** 2026-04-25
**Nächste PVA:** 2026-05-23

## Lernziel

- Ich kann Microservices- und Service-based Architekturen entwerfen.
- Ich bin fähig, die verschiedenen Protokolle wie SOAP, REST und gRPC zu nutzen.
- Ich kann Service-Discovery und Service-Mesh für GenAI entwickeln.
- Ich kann mit KI flexible Microservice-Architekturen bauen.
- Ich kann mit Spring-AI und Koog AI Agenten bauen.

## Auftrag (Moodle)

Wahrscheinlich sind Sie mit einer monolithischen oder einer einfachen Schichtenarchitektur im letzten Block gestartet, um das Frontend zu implementieren. Nun möchten wir die Services schrittweise in unabhängige Microservices auftrennen und verschiedene Technologien verwenden, diese flexibler und resilienter zu machen; resilienter — beispielsweise durch eine asynchrone Kommunikation mittels einer Queue. Wir wollen aber auch die KI selbst in den Services verwenden, um diese intelligenter zu gestalten. Nutzen Sie OpenAPI und MicroProfile REST Clients, um die Konsistenz zwischen Server und Client sicherzustellen. Am Ende dieser Nacharbeit haben Sie eine verteilte Lösung, in der Ihre Microservices fiktive oder statische Daten zurückgeben.

**Termin:** Bis zur nächsten Präsenzveranstaltung (2026-05-23).

**Reflexion & Auswertung:** Das Ergebnis dieser Projektarbeit ist Grundlage für den nächsten Block, wo die Lösung um die Persistenzschicht erweitert wird.

> **FlowHub-Kontext:** ADR 0002 (Accepted, 2026-04-17) hält fest, dass FlowHub ein Modular Monolith bleibt. Statt physischem Service-Split wird die Auftrennung über eine **MassTransit-basierte Async-Pipeline** (Capture-Enrichment, Skill-Routing) und eine eigene **REST-API in `source/FlowHub.Api/`** für Nicht-UI-Clients realisiert. gRPC und Multi-Prozess-Deployment sind explizit out of scope. Die Lernziel-Bullets zu Spring-AI / Koog werden als Stack-neutrale Konzepte interpretiert und mit .NET-Äquivalenten (Microsoft.Extensions.AI / Semantic Kernel) abgedeckt.

---

## Bewertungskriterien (Block 3)

Pflichtcheck am Ende jeder Nachbereitung — die offizielle Moodle-Rubrik aus [[Bewertungskriterien]] für **diesen** Block durchgehen, bevor "fertig" geclaimed wird. Punkteangaben in Klammern zeigen Max-Score (volle Erfüllung).

> Quarkus/Jakarta-EE-Item ist für FlowHub (.NET-Stack) **nicht relevant** — bewusst ausgeklammert.

### Spezifikation

- [ ] **Use Cases & fachliche Anforderungen benannt (5)** — Capture submit/list/retry, Skill-Routing, Integration-Health, Enrichment-Pipeline (Server-seitig formalisiert)
- [ ] **NfA SMART spezifiziert (5)** — API-Latenz, Throughput, Retry-/DLQ-Verhalten, Verfügbarkeit, OpenAPI-Versionierungs-SLA
- [x] **Solution Vision beschrieben (5)** — Modular Monolith + REST + MassTransit-Async-Pipeline (ADR 0002 + ADR 0003); REST-API-Surface in `docs/design/api/api-surface.md`

### Entwurf

- [x] **Lösungsansatz & Architektur textuell + bildlich (7)** — ADR 0003 (Async Pipeline) ✅, ADR 0004 (KI-Integration) noch offen (Slice C)
- [ ] **Struktur / Verhalten / Interaktion (7)**:
  - Struktur: Modul-Diagramm (FlowHub.Web ↔ FlowHub.Api ↔ FlowHub.Core ↔ Skills/Integrations, Bus)
  - Verhalten: Sequence-/Activity-Diagramme für Capture-Enrichment + Skill-Routing
  - Interaktion: API-Surface (OpenAPI/Scalar) + Event-Kontrakte (`CaptureCreated`, `CaptureClassified`, …)
- [ ] **DB-Modell spezifiziert (3)** — auch wenn Persistenz erst Block 4: Entity-Skizze damit nichts überrascht

### Programmierung

- [x] **Code lesbar, dokumentiert, nach Layer/Modul/Sub-System strukturiert (7)** — Slice B: hexagonale Ports (`IClassifier`, `ISkillIntegration`), Pipeline-Modul, EventId-Namespacing (1000–1999 / 2000–2999), `LoggerMessage` source-gen
- [ ] ~~Quarkus / Jakarta EE / moderne Java-Konzepte~~ — N/A (Stack: .NET 10), siehe [[Bewertungskriterien]]
- [x] **Erkenntnisse aus der Programmierung dokumentiert (3)** — ADR 0003, `docs/ai-usage.md` (Reflexion-Sektion mit ⚠/❌ Tabelle); CHANGELOG noch offen
- [x] **Source in Git-Repository (2)** — `github.com/freaxnx01/FlowHub-CAS-AISE`; Slice-B-Branch `feat/block3-async-pipeline` lokal committed, Push folgt nach Final Code Review

### Validierung

- [ ] **Abnahmekriterien definiert (5)** — pro Use Case explizit (z.B. in ADR oder `docs/acceptance-criteria.md`) — **partial (Slice A)**: 17 integration-test cases in `tests/FlowHub.Api.IntegrationTests/` serve as executable Abnahmekriterien per captures endpoint; formal UC list still open
- [ ] **Test-Strategie + Technologien spezifiziert (5)** — `docs/spec/testing-strategy.md` existiert (Block-2-Stand), Slice-B-Tools (MassTransit Test Harness, `LoggerMessage` source-gen) noch nicht ergänzt — **Slice A adds**: `Microsoft.AspNetCore.Mvc.Testing` + `WebApplicationFactory<Program>` now in documented test stack (17 integration tests, 74 total)
- [x] **Unit-Tests programmiert (3)** — Slice B: 16 neue Tests (KeywordClassifier 4, CaptureServiceStub 6, Pipeline-Consumers 6) → 47 Tests insgesamt grün
- [ ] **Test-Ergebnisse dokumentiert (3)** — in ADR 0003 + `docs/ai-usage.md` skizziert; CHANGELOG `[Unreleased]` noch offen

### KI, Sub-Systeme & Reflexion

- [x] **KI-Werkzeug-Nutzung beschrieben (12)** ⭐ höchstgewichtetes Kriterium — `docs/ai-usage.md` (Living Doc): Tool-Stack, Workflow (brainstorming → writing-plans → SDD), generierter vs. handgeschriebener Anteil, notable Adaptationen die Subagents gefunden haben (z.B. Captive-Dependency-Trap)
- [ ] **Intelligente / flexible Services mit KI gebaut (6)** — Port `IClassifier` + Stub `KeywordClassifier` ✅; AI-Backed-Adapter via `Microsoft.Extensions.AI` ist Slice C
- [x] **Sub-Systeme als unabhängige Container deploybar (5)** — Sketch-Level: `docker-compose.yml` (web + rabbitmq + future api) im Repo; finale Block-5-Variante folgt
- [x] **KI-Erfahrungen als Fazit reflektiert (7)** — Reflexion-Sektion in `docs/ai-usage.md` mit ✅ / ⚠ / ❌ Auflistung; PVA-Folien-Material noch zu destillieren

---

## TODO

### Architektur & Entscheide

- [x] ADR 0003 — Async Messaging Pipeline (MassTransit Topology, Retry/DLQ, Fault-Observer; Outbox auf Block 4 verschoben)
- [ ] ADR 0004 — KI-Integration in Services (Provider, Abstraction, Prompt-/Cost-Strategie, Stack-Mapping Spring-AI → .NET) — Slice C
- [x] OpenAPI-Versionierungs- und Konsistenzstrategie festlegen (Scalar als UI, Refit/Kiota als Client-Generator) — **partial**: Scalar wired and serving `/scalar`; Refit/Kiota client generation deferred to Slice D

### REST-API (`source/FlowHub.Api/`)

- [x] Projekt scaffolden + in `FlowHub.slnx` registrieren
- [x] Endpoints: `GET/POST /api/captures`, `GET /api/captures/{id}`, `POST /api/captures/{id}/retry`, `GET /api/skills`, `GET /api/integrations` — **partial**: captures endpoints (GET /api/v1/captures, POST /api/v1/captures, GET /api/v1/captures/{id}, POST /api/v1/captures/{id}/retry) all landed; GET /api/skills and GET /api/integrations **deferred to v2** per Slice A spec D7
- [x] FluentValidation am Boundary, ProblemDetails (RFC 9457) für Fehler
- [x] OpenAPI + Scalar UI (`/scalar`)
- [x] Stub-Daten-Quelle (Bogus/In-Memory aus Block 2 wiederverwenden) — keine echte Persistenz (Block 4)

### Async-Pipeline (MassTransit)

- [x] MassTransit + In-Memory-Transport in `FlowHub.Web` registriert — `FlowHub.Api`-Wiring kommt mit Slice A
- [x] Event-Kontrakte definiert: `CaptureCreated`, `CaptureClassified` — `SkillRoutingRequested` + `IntegrationCallFailed` bewusst gestrichen (ADR 0003 D2: Routing-Consumer schreibt inline; Failures sind `Fault<CaptureClassified>`)
- [x] Consumers: `CaptureEnrichmentConsumer`, `SkillRoutingConsumer` (statt `…Handler`), `LifecycleFaultObserver`
- [x] Retry-/DLQ-Policy + Test Harness Tests — per-Consumer-Retry (100/500ms enrichment, 500/2000/5000ms routing); Fault-Observer ohne Retry; 6 Tests
- [x] RabbitMQ-Profil vorbereiten — Docker-Compose-Sketch im Repo; lokales Aktivieren wird in Block 5 fertiggestellt

### KI in Services

- [ ] `Microsoft.Extensions.AI` (oder Semantic Kernel) als Abstraktion einbinden — Slice C
- [x] Klassifikator-Stub: Capture → vorgeschlagene Tags / Skill — `KeywordClassifier` (deterministische URL/todo-Heuristik, 4 Tests grün); Mock-Provider via NSubstitute in Pipeline-Tests
- [ ] Reflexion zu Spring-AI / Koog vs. .NET-Stack — kurz in ADR 0003 angerissen, eigenes Doc folgt mit ADR 0004

### Typed Clients (Konsistenz Server↔Client)

- [ ] Refit (oder Kiota) als MicroProfile-REST-Client-Äquivalent evaluieren und entscheiden
- [ ] Generierter/typed Client für `FlowHub.Api` aus OpenAPI
- [ ] Blazor-Pages über typed Client gegen die REST-API verdrahten (statt direkten Stub-Service-Calls)

### Validierung & Tests

- [ ] Test-Strategie als Dokument (`docs/test-strategy.md` o.ä.)
- [ ] Akzeptanzkriterien je Use Case (in den ADRs oder eigenem Doc)
- [x] Unit-Tests für Handlers / Validators — Slice B: KeywordClassifier (4), CaptureServiceStub (6), Pipeline-Consumers (6); Validators kommen mit Slice A
- [x] Component-Tests für API-Endpoints (`Microsoft.AspNetCore.Mvc.Testing`) — Slice A: 17 integration tests via `WebApplicationFactory<Program>` in `tests/FlowHub.Api.IntegrationTests/`
- [x] MassTransit Test Harness für Consumers — `PipelineTestBase` + 6 Harness-basierte Tests (Enrichment, Routing, Fault-Observer)
- [x] `dotnet test` voll grün — 47/47, Build mit warnings-as-errors clean; CHANGELOG-Eintrag noch offen

### Spezifikation & Dokumentation

- [ ] Use-Case-Liste ergänzen / aktualisieren (`vault/Projektarbeit/`)
- [ ] SMART NfAs für API + Async-Pipeline festhalten
- [ ] DB-Modell-Skizze (Vorbereitung für Block 4)
- [ ] CHANGELOG `[Unreleased]` mit Block-3-Deliverables füllen

### 🚫 Out of Scope (Block 3) — geparkt

- Physischer Microservice-Split / Multi-Prozess-Deployment (out of scope per ADR 0002)
- gRPC in FlowHub (POC-only, siehe `poc/restful-api-playground/`)
- Echte Persistenz → Block 4
- Authentik / OIDC → Block 5
- Service Mesh / Service Discovery in echtem Cluster → Block 5

---

## Verweise

- Repo: [[Repository]] — `github.com/freaxnx01/FlowHub-CAS-AISE`
- Block 3 Vorbereitung: [[03 Service - a) Vorbereitung]]
- Block 2 Nachbereitung: [[02 Frontend - c) Nachbereitung]]
- ADR 0001: `docs/adr/0001-frontend-render-mode-and-architecture.md`
- ADR 0002: `docs/adr/0002-service-architecture-and-async-communication.md`
- POC: `poc/restful-api-playground/` (REST + gRPC + RabbitMQ Sandbox)
- Bewertungskriterien: `.ai/cas-instructions.md` (Section "Grading")

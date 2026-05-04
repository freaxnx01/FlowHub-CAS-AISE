---
tags:
  - claude-generated
  - claude-updated
updated: 2026-05-04
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

- [x] **Use Cases & fachliche Anforderungen benannt (5)** — UC-01..UC-07 (Web UI / Telegram / Dashboard) + UC-08..UC-11 (REST submit, async pipeline, AI fallback, retry) in `docs/spec/use-cases.md`
- [x] **NfA SMART spezifiziert (5)** — NF-01..NF-08 (Block 2 UI) + NF-09..NF-13 (API latency, retry budget, AI fallback rate, AI cost, OpenAPI versioning SLA) in `docs/spec/use-cases.md`
- [x] **Solution Vision beschrieben (5)** — Modular Monolith + REST + MassTransit-Async-Pipeline (ADR 0002 + ADR 0003); REST-API-Surface in `docs/design/api/api-surface.md`

### Entwurf

- [x] **Lösungsansatz & Architektur textuell + bildlich (7)** — ADR 0003 (Async Pipeline) ✅, ADR 0004 (KI-Integration) ✅ (landed at `docs/adr/0004-ai-integration-in-services.md`, Slice C)
- [x] **Struktur / Verhalten / Interaktion (7)**:
  - Struktur: System-Context (`docs/spec/system-context.md`, C4 L1) + ADR 0002 module split
  - Verhalten: Sequence-Diagramme in `docs/design/sequences/` (Capture-Enrichment happy + AI fallback; Skill-Routing happy + retry exhaustion)
  - Interaktion: API-Surface (`docs/design/api/api-surface.md`, OpenAPI/Scalar) + Event-Kontrakte (`CaptureCreated`, `CaptureClassified`, `Fault<CaptureClassified>`)
- [x] **DB-Modell spezifiziert (3)** — Block-4-preview Entity-Skizze in `docs/design/db/entities.md` (Mermaid ER + indexes + deferred-decisions list)

### Programmierung

- [x] **Code lesbar, dokumentiert, nach Layer/Modul/Sub-System strukturiert (7)** — Slice B: hexagonale Ports (`IClassifier`, `ISkillIntegration`), Pipeline-Modul, EventId-Namespacing (1000–1999 / 2000–2999), `LoggerMessage` source-gen
- [x] ~~Quarkus / Jakarta EE / moderne Java-Konzepte~~ — N/A (Stack: .NET 10), siehe [[Bewertungskriterien]]; consciously skipped, will be noted in submission PDF
- [x] **Erkenntnisse aus der Programmierung dokumentiert (3)** — ADR 0003, `docs/ai-usage.md` (Reflexion-Sektion mit ⚠/❌ Tabelle); CHANGELOG noch offen
- [x] **Source in Git-Repository (2)** — `github.com/freaxnx01/FlowHub-CAS-AISE`; Slice-B-Branch `feat/block3-async-pipeline` lokal committed, Push folgt nach Final Code Review

### Validierung

- [x] **Abnahmekriterien definiert (5)** — UC-08..UC-11 in `docs/spec/use-cases.md` carry their own acceptance criteria; 17 integration tests in `tests/FlowHub.Api.IntegrationTests/` + 25 unit tests in `tests/FlowHub.Web.ComponentTests/Ai/` serve as executable acceptance per endpoint / classifier behaviour
- [x] **Test-Strategie + Technologien spezifiziert (5)** — `docs/spec/testing-strategy.md` updated to Block-3 reality: 99 default-suite + 4 trait-gated live tests; tools (xunit, bUnit, NSubstitute, FluentAssertions, `WebApplicationFactory<Program>`, MassTransit Test Harness, Xunit.SkippableFact) and Slice-C-specific patterns (NSubstitute on `IChatClient`, `[SkippableFact]`, `Category=AI` trait filter) documented
- [x] **Unit-Tests programmiert (3)** — 99 default-suite tests passing (82 component + 17 API integration) + 4 trait-gated live tests (Slice C). Build clean under warnings-as-errors
- [x] **Test-Ergebnisse dokumentiert (3)** — `CHANGELOG.md` `[Unreleased]` populated with Slice A/B/C deliverables; ADR 0003 / 0004 reference test counts + EventId namespacing; `docs/ai-usage.md` per-slice retros include test outcomes

### KI, Sub-Systeme & Reflexion

- [x] **KI-Werkzeug-Nutzung beschrieben (12)** ⭐ höchstgewichtetes Kriterium — `docs/ai-usage.md` (Living Doc): Tool-Stack, Workflow (brainstorming → writing-plans → SDD), generierter vs. handgeschriebener Anteil, notable Adaptationen die Subagents gefunden haben (z.B. Captive-Dependency-Trap)
- [x] **Intelligente / flexible Services mit KI gebaut (6)** — `KeywordClassifier` (deterministische Heuristik) ✅ + `AiClassifier` (Anthropic Haiku 4.5 / OpenRouter Llama 3.1 70B via MEAI) ✅; graceful fallback bei AI-Fehlern dokumentiert (ADR 0004, EventId 3010)
- [x] **Sub-Systeme als unabhängige Container deploybar (5)** — Sketch-Level: `docker-compose.yml` (web + rabbitmq + future api) im Repo; finale Block-5-Variante folgt
- [x] **KI-Erfahrungen als Fazit reflektiert (7)** — Reflexion-Sektion in `docs/ai-usage.md` mit ✅ / ⚠ / ❌ Auflistung; PVA-Folien-Material noch zu destillieren

---

## TODO

### Architektur & Entscheide

- [x] ADR 0003 — Async Messaging Pipeline (MassTransit Topology, Retry/DLQ, Fault-Observer; Outbox auf Block 4 verschoben)
- [x] ADR 0004 — KI-Integration in Services (Provider, Abstraction, Prompt-/Cost-Strategie, Stack-Mapping Spring-AI → .NET) — landed at `docs/adr/0004-ai-integration-in-services.md`
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

- [x] `Microsoft.Extensions.AI` (oder Semantic Kernel) als Abstraktion einbinden — landed in `source/FlowHub.AI/` via `IChatClient`; Anthropic + OpenRouter adapters (Slice C)
- [x] Klassifikator-Stub: Capture → vorgeschlagene Tags / Skill — `KeywordClassifier` (deterministische URL/todo-Heuristik, 4 Tests grün); Mock-Provider via NSubstitute in Pipeline-Tests
- [x] Reflexion zu `Microsoft.Extensions.AI` / Semantic Kernel im .NET-Stack — captured in ADR 0004 §"Reflexion" + `docs/ai-usage.md` Slice-C Reflexion-Sektion

### Typed Clients (Konsistenz Server↔Client) — deferred

> **Decision (2026-05-04):** Deferred indefinitely. Not on the Beta-MVP path — the Blazor UI talks to `ICaptureService` in-process (per ADR 0001 D2), so a typed REST client is only needed by future external consumers (Telegram bot, mobile). When that lands, evaluate Refit vs Kiota then; today there is no live consumer to wire.

- [x] ~~Refit (oder Kiota) als MicroProfile-REST-Client-Äquivalent evaluieren und entscheiden~~ — deferred (no live consumer)
- [x] ~~Generierter/typed Client für `FlowHub.Api` aus OpenAPI~~ — deferred (no live consumer)
- [x] ~~Blazor-Pages über typed Client gegen die REST-API verdrahten (statt direkten Stub-Service-Calls)~~ — deferred; ADR 0001 D2 specifies UI uses in-process services, not the REST API

### Validierung & Tests

- [x] Test-Strategie als Dokument — lives at `docs/spec/testing-strategy.md`; refreshed for Block 3 reality (99 default-suite + 4 trait-gated; tools, patterns, deferrals)
- [x] Akzeptanzkriterien je Use Case — UC-08..UC-11 in `docs/spec/use-cases.md` carry Postcondition / Error sections; 17 API integration tests + 25 Slice-C unit tests serve as executable acceptance per endpoint / classifier behaviour
- [x] Unit-Tests für Handlers / Validators — Slice B: KeywordClassifier (4), CaptureServiceStub (6), Pipeline-Consumers (6); Validators kommen mit Slice A
- [x] Component-Tests für API-Endpoints (`Microsoft.AspNetCore.Mvc.Testing`) — Slice A: 17 integration tests via `WebApplicationFactory<Program>` in `tests/FlowHub.Api.IntegrationTests/`
- [x] MassTransit Test Harness für Consumers — `PipelineTestBase` + 6 Harness-basierte Tests (Enrichment, Routing, Fault-Observer)
- [x] `dotnet test` voll grün — 47/47, Build mit warnings-as-errors clean; CHANGELOG-Eintrag noch offen

### Spezifikation & Dokumentation

- [x] Use-Case-Liste ergänzen / aktualisieren — UC-08..UC-11 in `docs/spec/use-cases.md`
- [x] SMART NfAs für API + Async-Pipeline festhalten — NF-09..NF-13 in `docs/spec/use-cases.md`
- [x] DB-Modell-Skizze (Vorbereitung für Block 4) — `docs/design/db/entities.md`
- [x] CHANGELOG `[Unreleased]` mit Block-3-Deliverables füllen — Slice A/B/C entries landed

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

---
tags:
  - claude-generated
updated: 2026-04-29
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
- [ ] **Solution Vision beschrieben (5)** — Modular Monolith + REST + MassTransit-Async-Pipeline (Verweis auf ADR 0002, ggf. Update für Block 3)

### Entwurf

- [ ] **Lösungsansatz & Architektur textuell + bildlich (7)** — ADR 0003 (Async Pipeline) + ADR 0004 (KI-Integration) inkl. Diagramme
- [ ] **Struktur / Verhalten / Interaktion (7)**:
  - Struktur: Modul-Diagramm (FlowHub.Web ↔ FlowHub.Api ↔ FlowHub.Core ↔ Skills/Integrations, Bus)
  - Verhalten: Sequence-/Activity-Diagramme für Capture-Enrichment + Skill-Routing
  - Interaktion: API-Surface (OpenAPI/Scalar) + Event-Kontrakte (`CaptureCreated`, `CaptureClassified`, …)
- [ ] **DB-Modell spezifiziert (3)** — auch wenn Persistenz erst Block 4: Entity-Skizze damit nichts überrascht

### Programmierung

- [ ] **Code lesbar, dokumentiert, nach Layer/Modul/Sub-System strukturiert (7)**
- [ ] ~~Quarkus / Jakarta EE / moderne Java-Konzepte~~ — N/A (Stack: .NET 10), siehe [[Bewertungskriterien]]
- [ ] **Erkenntnisse aus der Programmierung dokumentiert (3)** — CHANGELOG, ADRs, ggf. `docs/insights/block-3.md`
- [ ] **Source in Git-Repository (2)** — `github.com/freaxnx01/FlowHub-CAS-AISE`, alle Block-3-Commits gepusht

### Validierung

- [ ] **Abnahmekriterien definiert (5)** — pro Use Case explizit (z.B. in ADR oder `docs/acceptance-criteria.md`)
- [ ] **Test-Strategie + Technologien spezifiziert (5)** — `docs/test-strategy.md`: xUnit, FluentAssertions, NSubstitute, bUnit, ASP.NET Mvc.Testing, MassTransit Test Harness
- [ ] **Unit-Tests programmiert (3)** — Handlers, Validators, Consumers, API-Endpoints
- [ ] **Test-Ergebnisse dokumentiert (3)** — Counts + ggf. Coverage in CHANGELOG / `[Unreleased]`

### KI, Sub-Systeme & Reflexion

- [ ] **KI-Werkzeug-Nutzung beschrieben (12)** ⭐ höchstgewichtetes Kriterium — welche Tools (Claude Code, Copilot, ChatGPT), welche Aufgaben, Prompt-Strategien, generierter vs. handgeschriebener Anteil. Doku in `docs/ai-usage.md` oder pro Block in `docs/insights/`
- [ ] **Intelligente / flexible Services mit KI gebaut (6)** — mind. ein Service nutzt KI zur Laufzeit (Capture-Klassifikation via `Microsoft.Extensions.AI`)
- [ ] **Sub-Systeme als unabhängige Container deploybar (5)** — `FlowHub.Web` und `FlowHub.Api` als getrennte Container; RabbitMQ als Container; Docker-Compose-Profil dokumentiert. ⚠️ **Spannung mit ADR 0002:** Modular Monolith bleibt im Code, aber die Deployment-Topologie kann mehrere Container zeigen — Block 5 ist die finale Stufe, in Block 3 mind. den Plan + Compose-Skizze festhalten
- [ ] **KI-Erfahrungen als Fazit reflektiert (7)** — Block-3-Reflexion: was hat funktioniert, was nicht, wo war menschliche Korrektur nötig, lessons learned. Auch geeignet als Folien-Material für PVA 2026-05-23

---

## TODO

### Architektur & Entscheide

- [ ] ADR 0003 — Async Messaging Pipeline (MassTransit Transport, Topology, Retry/DLQ-Policy, Outbox?)
- [ ] ADR 0004 — KI-Integration in Services (Provider, Abstraction, Prompt-/Cost-Strategie, Stack-Mapping Spring-AI → .NET)
- [ ] OpenAPI-Versionierungs- und Konsistenzstrategie festlegen (Scalar als UI, Refit/Kiota als Client-Generator)

### REST-API (`source/FlowHub.Api/`)

- [ ] Projekt scaffolden + in `FlowHub.slnx` registrieren
- [ ] Endpoints: `GET/POST /api/captures`, `GET /api/captures/{id}`, `POST /api/captures/{id}/retry`, `GET /api/skills`, `GET /api/integrations`
- [ ] FluentValidation am Boundary, ProblemDetails (RFC 9457) für Fehler
- [ ] OpenAPI + Scalar UI (`/scalar`)
- [ ] Stub-Daten-Quelle (Bogus/In-Memory aus Block 2 wiederverwenden) — keine echte Persistenz (Block 4)

### Async-Pipeline (MassTransit)

- [ ] MassTransit + In-Memory-Transport in `FlowHub.Web` / `FlowHub.Api` registriert
- [ ] Event-Kontrakte definiert: `CaptureCreated`, `CaptureClassified`, `SkillRoutingRequested`, `IntegrationCallFailed`
- [ ] Consumers: `CaptureEnrichmentConsumer` (Classification + Tagging), `SkillRoutingHandler`
- [ ] Retry-/DLQ-Policy + Test Harness Tests
- [ ] RabbitMQ-Profil vorbereiten (Docker-Compose-Snippet, lokal aktivierbar)

### KI in Services

- [ ] `Microsoft.Extensions.AI` (oder Semantic Kernel) als Abstraktion einbinden
- [ ] Klassifikator-Stub: Capture → vorgeschlagene Tags / Skill — mit Mock-Provider testbar
- [ ] Reflexion zu Spring-AI / Koog vs. .NET-Stack (kurz dokumentieren, warum nicht JVM)

### Typed Clients (Konsistenz Server↔Client)

- [ ] Refit (oder Kiota) als MicroProfile-REST-Client-Äquivalent evaluieren und entscheiden
- [ ] Generierter/typed Client für `FlowHub.Api` aus OpenAPI
- [ ] Blazor-Pages über typed Client gegen die REST-API verdrahten (statt direkten Stub-Service-Calls)

### Validierung & Tests

- [ ] Test-Strategie als Dokument (`docs/test-strategy.md` o.ä.)
- [ ] Akzeptanzkriterien je Use Case (in den ADRs oder eigenem Doc)
- [ ] Unit-Tests für Handlers / Validators
- [ ] Component-Tests für API-Endpoints (`Microsoft.AspNetCore.Mvc.Testing`)
- [ ] MassTransit Test Harness für Consumers
- [ ] `dotnet test` voll grün, Resultate in CHANGELOG dokumentieren

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

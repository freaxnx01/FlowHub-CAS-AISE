---
tags:
  - claude-updated
updated: 2026-04-18
---

# Block 3 — Services · Vorbereitung

**Phase budget:** 30 h
**Letzte PVA:** 2026-03-21 (Block 2 Frontend)
**Nächste PVA:** 2026-04-25 (Block 3 Services)
**Phase range (nach Easter-Split):** 2026-04-08 → 2026-04-24

## Aufträge (Moodle)

Zwei offizielle Vorbereitungs-Aufträge aus Moodle:

1. **Leseauftrag: Microservices- und Service-based-Architekturen** — `_files/Moodle/Modul/3-Service/pdf/W4B-C-AS001.AISE.ZH-Sa-1.PVA.FS26_ Leseauftrag_ Microservices- und Service-based-Architekturen _ Moodle.pdf`
2. **RESTful API (praktische Übung)** — `_files/Moodle/Modul/3-Service/pdf/W4B-C-AS001.AISE.ZH-Sa-1.PVA.FS26_ RESTful API _ Moodle.pdf`

---

## TODO

### 📖 Leseauftrag

> Quarkus in Action Kapitel sind bewusst **ausgeschlossen** — Stack-Mismatch mit .NET/Blazor.

#### [1] Beyond Vibe Coding

- [x] Kapitel 5: Understand Generated Code — Review, Refine, Own
- [x] Kapitel 6: AI-Driven Prototyping — Tools and Technique

#### [2] Coding with AI

- [x] Kapitel 5: Using Blackbox AI to generate base code

#### [3] Head First Software Architecture

- [x] Kapitel 5: Architectural Styles — Categorization and Philosophies
- [x] Kapitel 10: Microservices Architecture — Bit by Bit
- [x] Kapitel 11: Event-Driven Architecture — Asynchronous Adventures

#### Leitfragen (aus Moodle) — beim Lesen im Kopf behalten

- [x] Nach welchen Kriterien lassen sich Architekturstile kategorisieren?
- [x] Was zeichnet eine Microservice-Architektur aus, und wann ist sie besonders geeignet?
- [x] Was sind Vor- und Nachteile asynchroner Kommunikation?
- [x] Welche Probleme können bei KI-generiertem Code entstehen, und wie vermeidet man sie?
- [x] Wie nutze ich AI-Prototyping, um schrittweise das gewünschte Resultat zu erhalten?

### 🛠 Praktische Übung: RESTful API (sync + async)

> Umsetzung in **.NET 10 / ASP.NET Core**, nicht Quarkus (Stack-Alignment mit FlowHub). Der Moodle-Auftrag nennt Quarkus nur als Beispiel-Stack — die Lernziele sind Stack-neutral (SOAP/REST/gRPC, sync/async, Event-basiert, Resilienz).
>
> **Ausserhalb des FlowHub-Repos** (Moodle-Auftrag empfiehlt explizit "dies ausserhalb Ihrer Projektarbeit zu tun"). Zielort: separates Sandbox-Repo oder `poc/` Unterordner.

**Scaffold-Status:** ✅ gebaut mit Claude am 2026-04-13/14, liegt in `poc/restful-api-playground/` im FlowHub-Repo.

Option B gewählt: **OrderService + NotificationService + RabbitMQ**.
- OrderService: REST (`:5001`) + gRPC Server (`:5003`) + MassTransit Publisher
- NotificationService: REST (`:5002`) + MassTransit Consumer + gRPC Client
- RabbitMQ via Docker Compose (`:5672` + `:15672` Management UI)
- 10 xUnit-Tests (OrderStore, OrderGrpcService, NotificationStore, OrderPlacedConsumer mit MassTransit Test Harness)

### ✅ Done (Scaffold)

- [x] Sandbox-Projekt unter `poc/restful-api-playground/` angelegt
- [x] **Synchroner REST-Service** — OrderService ASP.NET Core Minimal API + Scalar UI
- [x] **gRPC-Service + Client** — `Grpc.AspNetCore` (Server in OrderService, Client via `AddGrpcClient` in NotificationService)
- [x] **Async / Event-basiert** — MassTransit + RabbitMQ (Docker)
- [x] **Tests** — 10 Tests, alle grün (`make test` im Playground-Ordner)

### ✅ Done — manueller End-to-End-Test

- [x] `docker --version` prüfen / Docker Desktop läuft
- [x] **Terminal 1:** `cd poc/restful-api-playground && make infra-up` (RabbitMQ starten)
- [x] **Terminal 2:** `make order-service` (auf :5001 REST + :5003 gRPC)
- [x] **Terminal 3:** `make notification-service` (auf :5002)
- [x] **Terminal 4:** `make demo` — POSTet Order, wartet 2s, GET Notifications
- [x] Erwartetes Ergebnis: Notification enthält `"3x Widget for Alice"`
- [x] Rabbit Management UI öffnen: http://localhost:15672 (guest/guest) — Queue `OrderPlacedConsumer` mit 1 verarbeiteter Message sehen
- [x] Scalar docs anschauen: http://localhost:5001/scalar und http://localhost:5002/scalar
- [x] `make infra-down` zum Aufräumen

### 🔲 Offen — Reflexion / KI-Erfahrungen

- [ ] KI-Assistent (Claude Code) Erfahrungen notieren für PVA-Reflexion
- [ ] Kurze Notiz in `poc/restful-api-playground/REFLECTION.md` oder hier:
  - Wann detaillierte Arbeitsanweisung an KI, wann schrittweise Delegation?
  - Welche Probleme bei generiertem Code beobachtet? (z.B. CentralPackageManagement-Fehler, `AddOpenApi` fehlende Package, Grpc.Tools PrivateAssets)
  - Sync vs. async — wo welche Variante? (REST für Client-Aufruf, gRPC für Service-zu-Service typed, RabbitMQ für entkoppelte Broadcasts)

### 📝 Projektarbeit-Vorbereitung (optional, für Block 3 Nachbereitung)

> Kein Moodle-Pflichtteil, aber sinnvoll um in die Nachbereitung mit klarem Plan zu starten.

- [ ] FlowHub API-Surface skizzieren — welche Endpoints brauchen die bestehenden Blazor-Pages?
  - `GET /api/captures`, `POST /api/captures`, `GET /api/captures/{id}`, `POST /api/captures/{id}/retry`, …
  - `GET /api/skills`, `GET /api/integrations`
- [x] Entscheiden: Bleibt FlowHub ein Modular Monolith (ADR 0001), oder echte Microservice-Aufteilung für Block 3?
  - Moodle-Auftrag fordert "Microservices", ADR 0001 sagt Modular Monolith. → Entscheid in ADR 0002 dokumentiert.
  - **Entscheid:** `docs/adr/0002-service-architecture-and-async-communication.md` (Status: **Accepted**, 2026-04-17). Modular Monolith bleibt; MassTransit-Async-Pipeline für Capture-Enrichment / Skill-Routing; REST-API für Nicht-UI-Clients in `source/FlowHub.Api/`; gRPC und physischer Service-Split abgelehnt.
- [ ] Kandidaten für asynchrone Kommunikation in FlowHub identifizieren (z.B. Capture-Enrichment, Skill-Routing als Event-Pipeline)

---

## Verweise

- Repo: [[Repository]] — `github.com/freaxnx01/FlowHub-CAS-AISE`
- ADR 0001: `docs/adr/0001-frontend-render-mode-and-architecture.md`
- ADR 0002: `docs/adr/0002-service-architecture-and-async-communication.md`
- POC-Reflexion: `poc/restful-api-playground/REFLECTION.md`
- Block 2 Nachbereitung: [[02 Frontend - c) Nachbereitung]]
- Block 3 Nachbereitung: [[03 Service - c) Nachbereitung]]

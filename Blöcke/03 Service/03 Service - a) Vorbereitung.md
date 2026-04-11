---
tags:
  - claude-updated
updated: 2026-04-11
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

- [ ] Kapitel 5: Understand Generated Code — Review, Refine, Own
- [ ] Kapitel 6: AI-Driven Prototyping — Tools and Technique

#### [2] Coding with AI

- [ ] Kapitel 5: Using Blackbox AI to generate base code

#### [3] Head First Software Architecture

- [ ] Kapitel 5: Architectural Styles — Categorization and Philosophies
- [ ] Kapitel 10: Microservices Architecture — Bit by Bit
- [ ] Kapitel 11: Event-Driven Architecture — Asynchronous Adventures

#### Leitfragen (aus Moodle) — beim Lesen im Kopf behalten

- [ ] Nach welchen Kriterien lassen sich Architekturstile kategorisieren?
- [ ] Was zeichnet eine Microservice-Architektur aus, und wann ist sie besonders geeignet?
- [ ] Was sind Vor- und Nachteile asynchroner Kommunikation?
- [ ] Welche Probleme können bei KI-generiertem Code entstehen, und wie vermeidet man sie?
- [ ] Wie nutze ich AI-Prototyping, um schrittweise das gewünschte Resultat zu erhalten?

### 🛠 Praktische Übung: RESTful API (sync + async)

> Umsetzung in **.NET 10 / ASP.NET Core**, nicht Quarkus (Stack-Alignment mit FlowHub). Der Moodle-Auftrag nennt Quarkus nur als Beispiel-Stack — die Lernziele sind Stack-neutral (SOAP/REST/gRPC, sync/async, Event-basiert, Resilienz).
>
> **Ausserhalb des FlowHub-Repos** (Moodle-Auftrag empfiehlt explizit "dies ausserhalb Ihrer Projektarbeit zu tun"). Zielort: separates Sandbox-Repo oder `poc/` Unterordner.

- [ ] Sandbox-Projekt anlegen (z.B. `poc/FlowHub.Services.Playground/` oder separates Repo)
- [ ] **Synchroner REST-Service** — ASP.NET Core Minimal API, liefert fiktive Daten, Scalar/Swagger UI
- [ ] **REST-Client** — `HttpClient` + `IHttpClientFactory` oder Refit, typed client gegen obigen Service
- [ ] **gRPC-Service + Client** — `Grpc.AspNetCore`, Proto-File definieren, generierte Stubs
- [ ] **Async / Event-basiert** — mindestens eine dieser Varianten:
  - [ ] In-Process Event Bus: `MediatR` Publish/Notify oder `System.Threading.Channels`
  - [ ] Out-of-Process Messaging: MassTransit mit RabbitMQ (Docker Container lokal)
- [ ] KI-Assistent (Claude Code) für Generierung einsetzen — Erfahrungen notieren für PVA-Reflexion
- [ ] **Reflexion dokumentieren** (kurze Notiz in diesem File oder `notes.md` im Sandbox-Repo):
  - Wann detaillierte Arbeitsanweisung an KI, wann schrittweise Delegation?
  - Welche Probleme bei generiertem Code beobachtet?
  - Sync vs. async — wo welche Variante?

### 📝 Projektarbeit-Vorbereitung (optional, für Block 3 Nachbearbeitung)

> Kein Moodle-Pflichtteil, aber sinnvoll um in die Nachbearbeitung mit klarem Plan zu starten.

- [ ] FlowHub API-Surface skizzieren — welche Endpoints brauchen die bestehenden Blazor-Pages?
  - `GET /api/captures`, `POST /api/captures`, `GET /api/captures/{id}`, `POST /api/captures/{id}/retry`, …
  - `GET /api/skills`, `GET /api/integrations`
- [ ] Entscheiden: Bleibt FlowHub ein Modular Monolith (ADR 0001), oder echte Microservice-Aufteilung für Block 3?
  - Moodle-Auftrag fordert "Microservices", ADR 0001 sagt Modular Monolith. → Entscheid in ADR 0002 dokumentieren.
- [ ] Kandidaten für asynchrone Kommunikation in FlowHub identifizieren (z.B. Capture-Enrichment, Skill-Routing als Event-Pipeline)

---

## Verweise

- Repo: [[Repository]] — `github.com/freaxnx01/FlowHub-CAS-AISE`
- ADR 0001: `docs/adr/0001-frontend-render-mode-and-architecture.md`
- Block 2 Nachbearbeitung: [[02 Frontend - c) Nachbearbeitung]]
- Block 3 Nachbearbeitung: [[03 Service - c) Nachbearbeitung]]

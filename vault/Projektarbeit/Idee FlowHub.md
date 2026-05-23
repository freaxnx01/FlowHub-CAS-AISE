---
tags:
  - claude-updated
updated: 2026-05-23
---

## Name

- FlowHub
- IntelliFlow
## Claude Code Project Notes

```
Option C: Hybrid (MEINE EMPFEHLUNG!)
Skill Definition (skill.md):
```

## Skills

Neue Skill-Ideen:

- Kontakinfos (Adresse, Email, Phone) -> Google Contacts


---

AutoVerschopper
Quotes
Bücher
Geolocations, Besuchter Orte, Ferien, Ausflüge
Ausflüge J, Vorschläge nach Datum/Saison/Wetter, Auswahl in Kalender eintragen, gemachte nicht mehr vorschlagen, Fotos taggen
Heise ct artikel merken zum Nachschlagen
Shopping list, abgleich mit Amazon/Galaxus, inventory (homebox)
Movies
Wenn Einsortierungsskill fehlt, autom. Erstellung, Vorschlag
Abgleich mit todoist
Anbindung Steam, Games
Alle Infoschnipsel welche über Signal reinkommen
Bücher Sammlung
RAG auf Obsidian
DMS paperless-ngx
Gesundheitsdaten
Einsortierung kb
Belege/quittungen
Budget
Prefix, slash commands

## FlowHub - Management Summary

## Problem

Fragmentierte Workflows: Signal Notes → manuell sortieren → Copy-Paste in verschiedene Tools (Todoist, Read-Later, Kanban). Zeitverschwendung durch Kontext-Switching.

## Lösung

**FlowHub**: Intelligenter Integration-Hub, der bestehende Self-Hosted Services orchestriert. AI kategorisiert Input automatisch und routet an den richtigen Service.

## Architektur

**Core (Quarkus/Java):**

- API Gateway & Orchestrator
- AI Processing (Ollama - lokal, kostenlos)
- Integration Hub mit REST Clients
- Passbolt für Credentials

**Integriert:** Wallabag (Read-Later) | Vikunja (Tasks/Kanban) | n8n (Workflows) | Paperless-ngx | Obsidian (Doku)

## Workflow-Beispiel

```
Signal: "Inception - rewatch"
→ FlowHub empfängt
→ AI: "Movie"
→ Vikunja: Task erstellt in "Movies → To Watch"
```

## MVP (Phase 1)

Signal Input → AI Categorization → Wallabag/Vikunja Integration → Simple UI

## Evolution

Block 1-2: Modularer Monolith | Block 3: Microservices | Block 4: RAG (Homelab-Doku)

## CAS-Fit

✅ Verteilte Architektur | ✅ AI-Integration | ✅ Microservice-Evolution | ✅ Praxisrelevant

## Value

Automatisiert 80% der manuellen Workflow-Arbeit. Kostenlos (~$0-5). Demonstriert Enterprise-Integration-Patterns.

---

## Solution Vision — Stack-Update (Block 4, 2026-05)

Der "Architektur"-Abschnitt oben ist die ursprüngliche Skizze und nennt noch Quarkus/Java — das war die initiale Stackwahl vor Block 1. Während der Umsetzung wurde **.NET 10 / ASP.NET Core / Blazor** als Stack gewählt (siehe ADR 0001 für das Frontend-Rendering, ADR 0004 für die KI-Integration). Diese Vision-Notiz hält den heutigen Stand fest, damit das Dokument konsistent zu Code und Submission bleibt.

**Persistenzschicht (Block 4):** FlowHub speichert Captures, Skills, Skill-Runs, Integrations und deren Health-Samples in **PostgreSQL 17**, abstrahiert über **EF Core 10** (`Microsoft.EntityFrameworkCore` + `Npgsql.EntityFrameworkCore.PostgreSQL`). Die Domäne in `FlowHub.Core` definiert reine POCO-Typen und treibende Ports (`ICaptureService` etc.) sowie getriebene Ports (`ICaptureRepository`, `ISkillRepository`, `IIntegrationRepository`, `ISkillRunRepository`, `ITagRepository`, `IChannelRepository`). Die EF-Core-Adapter liegen ausschließlich in `FlowHub.Persistence` — das Repository-Pattern ist also bewusst pro Aggregate eingezogen, nicht generisch (ADR 0005 §3). Dynamische Abfragen werden über LINQ-Expression-Trees komponiert (`CaptureQueryBuilder`), Migrationen über `dotnet-ef` erzeugt und über einen separaten `flowhub.migrations`-Init-Container vor App-Start ausgerollt (12-Factor XII). Soft-Delete-Semantik trägt der `LifecycleStage`-Zustandsautomat (`Raw → Classified → Routed → Completed`, plus `Orphan` / `Unhandled`) — es gibt bewusst kein `IsDeleted`-Flag.

**Warum dieser Stack:** Der Wechsel zu .NET hat keine fachliche Vorgabe der Projektarbeit verletzt — die CAS-Inhalte (ORM-Abstraktion, dynamische Abfragen, Datenmodell-Antizipation, 12-Factor) übertragen sich 1:1 ins .NET-Ökosystem (EF Core ↔ JPA, LINQ-Expression-Trees ↔ Criteria API, `IServiceCollection`-DI ↔ CDI, Health-Endpoints ↔ MicroProfile Health). Die Stack-Mappings sind in jeder Block-Nachbereitung im Auftrags-Intro dokumentiert.